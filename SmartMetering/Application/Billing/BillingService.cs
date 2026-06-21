using System.Net;
using System.Text;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Properties;
using SmartMetering.Domain.Users;

namespace SmartMetering.Application.Billing;

public sealed class BillingService : IBillingService
{
    private readonly ITariffModelRepository _tariffs;
    private readonly IInvoiceRepository _invoices;
    private readonly ISmartMeterRepository _meters;
    private readonly IPropertyRepository _properties;
    private readonly IUserRepository _users;
    private readonly ITelemetryRepository _telemetry;
    private readonly IInvoiceDocumentStorage _documents;
    private readonly IEmailService _email;
    private readonly ILogger<BillingService> _logger;

    public BillingService(
        ITariffModelRepository tariffs,
        IInvoiceRepository invoices,
        ISmartMeterRepository meters,
        IPropertyRepository properties,
        IUserRepository users,
        ITelemetryRepository telemetry,
        IInvoiceDocumentStorage documents,
        IEmailService email,
        ILogger<BillingService> logger)
    {
        _tariffs = tariffs;
        _invoices = invoices;
        _meters = meters;
        _properties = properties;
        _users = users;
        _telemetry = telemetry;
        _documents = documents;
        _email = email;
        _logger = logger;
    }

    public async Task<IReadOnlyList<TariffModelDto>> GetTariffModelsAsync(CancellationToken ct = default)
    {
        var tariffs = await _tariffs.GetAllAsync(ct);
        return tariffs.Select(Map).ToList();
    }

    public async Task<TariffModelDto?> GetActiveTariffAsync(CancellationToken ct = default)
    {
        var tariff = await _tariffs.GetActiveAsync(ct);
        return tariff is null ? null : Map(tariff);
    }

    public async Task<Guid> CreateTariffModelAsync(CreateTariffModelRequest request, bool activate, CancellationToken ct = default)
    {
        ValidateTariff(request);

        var tariff = TariffModel.Create(
            request.Name,
            request.GreenLimitKwh,
            request.BlueLimitKwh,
            request.GreenHighPriceRsd,
            request.GreenLowPriceRsd,
            request.BlueHighPriceRsd,
            request.BlueLowPriceRsd,
            request.RedHighPriceRsd,
            request.RedLowPriceRsd,
            request.PowerPriceRsdPerKw,
            request.SupplierFeeRsd);

        if (activate)
        {
            // Deactivate the currently active model and persist BEFORE inserting the new active one.
            // The filtered unique index IX_TariffModels_IsActive ([IsActive] = 1) allows only one
            // active row, and EF gives no ordering guarantee between the deactivate and insert in a
            // single SaveChanges, so they must be separate round-trips.
            var existing = await _tariffs.GetAllAsync(ct);
            foreach (var item in existing.Where(t => t.IsActive))
            {
                item.Deactivate();
            }

            await _tariffs.SaveChangesAsync(ct);
            tariff.Activate();
        }

        await _tariffs.AddAsync(tariff, ct);
        await _tariffs.SaveChangesAsync(ct);
        return tariff.Id.Value;
    }

    public async Task ActivateTariffModelAsync(Guid tariffId, CancellationToken ct = default)
    {
        var tariff = await _tariffs.GetByIdAsync(EntityId.From(tariffId), ct)
            ?? throw new NotFoundException("Tarifni model nije pronadjen.");

        if (tariff.IsActive)
        {
            return;
        }

        // Deactivate the currently active model and persist FIRST. The filtered unique index
        // IX_TariffModels_IsActive ([IsActive] = 1) rejects two active rows, and EF doesn't
        // guarantee the deactivate UPDATE runs before the activate UPDATE in one SaveChanges batch.
        var all = await _tariffs.GetAllAsync(ct);
        foreach (var item in all.Where(t => t.IsActive))
        {
            item.Deactivate();
        }

        await _tariffs.SaveChangesAsync(ct);

        tariff.Activate();
        await _tariffs.SaveChangesAsync(ct);
    }

    public async Task<GeneratedInvoicesDto> GenerateMonthlyInvoicesAsync(int year, int month, CancellationToken ct = default)
    {
        if (month is < 1 or > 12)
        {
            throw new AppException("Mesec mora biti u opsegu 1-12.");
        }

        var tariff = await _tariffs.GetActiveAsync(ct)
            ?? throw new AppException("Nije definisan aktivan tarifni model.");

        var periodStart = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var periodEnd = periodStart.AddMonths(1);
        var meters = await _meters.GetPairedAsync(ct);
        var created = 0;
        var skipped = 0;

        foreach (var meter in meters)
        {
            if (await _invoices.ExistsAsync(meter.Id, year, month, ct))
            {
                skipped++;
                continue;
            }

            var property = await _properties.GetByIdAsync(meter.PropertyId, ct);
            if (property is null)
            {
                skipped++;
                continue;
            }

            var consumer = await _users.GetByIdAsync(property.OwnerId, ct);
            if (consumer is null)
            {
                skipped++;
                continue;
            }

            var readings = await _telemetry.GetForPeriodAsync(meter.Id, periodStart, periodEnd, ct);
            var previous = await _telemetry.GetPreviousBeforeAsync(meter.Id, periodStart, ct);
            var consumption = BillingCalculator.CalculateConsumption(readings, previous);
            var breakdown = BillingCalculator.CalculateInvoice(consumption, meter, tariff);

            var invoice = Invoice.Create(
                property.OwnerId,
                property.Id,
                meter.Id,
                tariff.Id,
                meter.SerialNumber,
                year,
                month,
                consumption.HighTariffKwh,
                consumption.LowTariffKwh,
                breakdown.GreenHighKwh,
                breakdown.GreenLowKwh,
                breakdown.BlueHighKwh,
                breakdown.BlueLowKwh,
                breakdown.RedHighKwh,
                breakdown.RedLowKwh,
                breakdown.GreenAmountRsd,
                breakdown.BlueAmountRsd,
                breakdown.RedAmountRsd,
                breakdown.FixedAmountRsd,
                breakdown.TotalAmountRsd);

            var textBlobName = $"invoices/{year}/{month:D2}/{invoice.Id.Value}.txt";
            var pdfBlobName = $"invoices/{year}/{month:D2}/{invoice.Id.Value}.pdf";
            var content = CreateInvoiceText(invoice, tariff, property, consumer);

            await _documents.SaveTextAsync(textBlobName, content, ct);
            await _documents.SavePdfAsync(pdfBlobName, content, ct);

            invoice.AttachDocuments(textBlobName, pdfBlobName);
            await _invoices.AddAsync(invoice, ct);
            await _invoices.SaveChangesAsync(ct);

            await TrySendInvoiceEmailAsync(consumer, invoice, content, ct);
            created++;
        }

        return new GeneratedInvoicesDto(year, month, created, skipped);
    }

    public async Task<InvoicePageDto> GetPropertyInvoicesAsync(
        EntityId ownerId,
        Guid propertyId,
        Guid? meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        var property = await _properties.GetByIdAsync(EntityId.From(propertyId), ct);
        if (property is null || !property.IsOwnedBy(ownerId))
        {
            throw new NotFoundException("Objekat nije pronadjen.");
        }

        EntityId? filteredMeterId = null;
        if (meterId is { } requestedMeterId)
        {
            var meter = await _meters.GetByIdAsync(EntityId.From(requestedMeterId), ct);
            if (meter is null || meter.PropertyId != property.Id)
            {
                throw new NotFoundException("Brojilo nije pronadjeno.");
            }

            filteredMeterId = meter.Id;
        }

        var from = NormalizeUtc(fromUtc);
        var to = NormalizeUtc(toUtc);
        if (from is not null && to is not null && to < from)
        {
            throw new AppException("Krajnji datum mora biti posle pocetnog datuma.");
        }

        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 50);
        var skip = (page - 1) * pageSize;
        var total = await _invoices.CountByPropertyAsync(ownerId, property.Id, filteredMeterId, from, to, ct);
        var items = await _invoices.GetByPropertyAsync(ownerId, property.Id, filteredMeterId, from, to, skip, pageSize, ct);

        return new InvoicePageDto(items.Select(Map).ToList(), page, pageSize, total);
    }

    public async Task<InvoiceFileDto> GetInvoicePdfAsync(EntityId ownerId, Guid invoiceId, CancellationToken ct = default)
    {
        var invoice = await _invoices.GetByIdAsync(EntityId.From(invoiceId), ct);
        if (invoice is null || invoice.ConsumerId != ownerId)
        {
            throw new NotFoundException("Racun nije pronadjen.");
        }

        var document = await _documents.DownloadAsync(invoice.PdfBlobName, ct)
            ?? throw new NotFoundException("PDF racuna nije pronadjen.");

        return new InvoiceFileDto(document.FileName, document.ContentType, document.Content);
    }

    private static void ValidateTariff(CreateTariffModelRequest request)
    {
        if (request.BlueLimitKwh <= request.GreenLimitKwh)
        {
            throw new AppException("Plava zona mora imati veci prag od zelene zone.");
        }

        var prices = new[]
        {
            request.GreenHighPriceRsd,
            request.GreenLowPriceRsd,
            request.BlueHighPriceRsd,
            request.BlueLowPriceRsd,
            request.RedHighPriceRsd,
            request.RedLowPriceRsd,
            request.PowerPriceRsdPerKw,
            request.SupplierFeeRsd,
        };

        if (prices.Any(p => p < 0))
        {
            throw new AppException("Cene ne mogu biti negativne.");
        }
    }

    private async Task TrySendInvoiceEmailAsync(User consumer, Invoice invoice, string content, CancellationToken ct)
    {
        try
        {
            await _email.SendAsync(
                consumer.Email,
                $"Smart Metering racun {invoice.Year}-{invoice.Month:D2}",
                $"<pre>{WebUtility.HtmlEncode(content)}</pre>",
                ct);

            invoice.MarkEmailSent();
            await _invoices.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Invoice {InvoiceId} was generated but email delivery to {Email} failed.",
                invoice.Id.Value,
                consumer.Email);
        }
    }

    private static string CreateInvoiceText(Invoice invoice, TariffModel tariff, Property property, User consumer)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SMART METERING - RACUN ZA ELEKTRICNU ENERGIJU");
        sb.AppendLine(new string('-', 58));
        sb.AppendLine($"Racun: {invoice.Id.Value}");
        sb.AppendLine($"Period: {invoice.Year}-{invoice.Month:D2}");
        sb.AppendLine($"Datum izdavanja (UTC): {invoice.IssuedAtUtc:yyyy-MM-dd HH:mm}");
        sb.AppendLine();
        sb.AppendLine($"Potrosac: {consumer.FullName}");
        sb.AppendLine($"Email: {consumer.Email}");
        sb.AppendLine($"Objekat: {property.Name}, {property.City}, {property.Address}");
        sb.AppendLine($"Brojilo: {invoice.SerialNumber}");
        sb.AppendLine();
        sb.AppendLine("Potrosnja po tarifama");
        sb.AppendLine($"VT: {Kwh(invoice.HighTariffKwh)}");
        sb.AppendLine($"NT: {Kwh(invoice.LowTariffKwh)}");
        sb.AppendLine($"Ukupno: {Kwh(invoice.TotalKwh)}");
        sb.AppendLine();
        sb.AppendLine("Potrosnja po zonama");
        sb.AppendLine($"Zelena: {Kwh(invoice.GreenKwh)} (VT {Kwh(invoice.GreenHighKwh)}, NT {Kwh(invoice.GreenLowKwh)})");
        sb.AppendLine($"Plava:  {Kwh(invoice.BlueKwh)} (VT {Kwh(invoice.BlueHighKwh)}, NT {Kwh(invoice.BlueLowKwh)})");
        sb.AppendLine($"Crvena: {Kwh(invoice.RedKwh)} (VT {Kwh(invoice.RedHighKwh)}, NT {Kwh(invoice.RedLowKwh)})");
        sb.AppendLine();
        sb.AppendLine("Obracun");
        sb.AppendLine($"Tarifni model: {tariff.Name}");
        sb.AppendLine($"Zelena zona: {Money(invoice.GreenAmountRsd)}");
        sb.AppendLine($"Plava zona:  {Money(invoice.BlueAmountRsd)}");
        sb.AppendLine($"Crvena zona: {Money(invoice.RedAmountRsd)}");
        sb.AppendLine($"Fiksni troskovi: {Money(invoice.FixedAmountRsd)}");
        sb.AppendLine(new string('-', 58));
        sb.AppendLine($"UKUPNO ZA UPLATU: {Money(invoice.TotalAmountRsd)}");
        sb.AppendLine($"Status: {invoice.Status}");
        return sb.ToString();
    }

    private static TariffModelDto Map(TariffModel t) =>
        new(
            t.Id.Value,
            t.Name,
            t.GreenLimitKwh,
            t.BlueLimitKwh,
            t.GreenHighPriceRsd,
            t.GreenLowPriceRsd,
            t.BlueHighPriceRsd,
            t.BlueLowPriceRsd,
            t.RedHighPriceRsd,
            t.RedLowPriceRsd,
            t.PowerPriceRsdPerKw,
            t.SupplierFeeRsd,
            t.IsActive,
            t.CreatedAtUtc,
            t.ActivatedAtUtc);

    private static InvoiceDto Map(Invoice i) =>
        new(
            i.Id.Value,
            i.PropertyId.Value,
            i.MeterId.Value,
            i.SerialNumber,
            i.Year,
            i.Month,
            i.IssuedAtUtc,
            i.HighTariffKwh,
            i.LowTariffKwh,
            i.GreenKwh,
            i.BlueKwh,
            i.RedKwh,
            i.TotalKwh,
            i.TotalAmountRsd,
            (int)i.Status);

    private static string Kwh(decimal value) => $"{value:0.###} kWh";

    private static string Money(decimal value) => $"{value:0.00} RSD";

    private static DateTime? NormalizeUtc(DateTime? value) =>
        value is null
            ? null
            : value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            };
}
