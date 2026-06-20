using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Network;

public sealed class NetworkOverviewService : INetworkOverviewService
{
    private readonly ISmartMeterRepository _meters;
    private readonly IMeterStatusRepository _statuses;
    private readonly IPropertyRepository _properties;
    private readonly IUserRepository _users;
    private readonly IInvoiceRepository _invoices;
    private readonly IAlertLogRepository _alerts;

    public NetworkOverviewService(
        ISmartMeterRepository meters,
        IMeterStatusRepository statuses,
        IPropertyRepository properties,
        IUserRepository users,
        IInvoiceRepository invoices,
        IAlertLogRepository alerts)
    {
        _meters = meters;
        _statuses = statuses;
        _properties = properties;
        _users = users;
        _invoices = invoices;
        _alerts = alerts;
    }

    public async Task<IReadOnlyList<MeterNetworkStatusDto>> GetMeterStatusesAsync(CancellationToken ct = default)
    {
        var meters = await _meters.GetAllAsync(ct);
        if (meters.Count == 0)
        {
            return [];
        }

        var statusesByMeter = (await _statuses.GetAllAsync(ct)).ToDictionary(s => s.MeterId);
        var propertiesById = (await _properties.GetAllAsync(ct)).ToDictionary(p => p.Id);
        var lastInvoiceByMeter = (await _invoices.GetAllAsync(ct))
            .GroupBy(i => i.MeterId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(i => i.Year).ThenByDescending(i => i.Month).First());

        var ownerNameCache = new Dictionary<EntityId, string>();
        var now = DateTime.UtcNow;
        var results = new List<MeterNetworkStatusDto>(meters.Count);

        foreach (var meter in meters)
        {
            propertiesById.TryGetValue(meter.PropertyId, out var property);
            statusesByMeter.TryGetValue(meter.Id, out var status);
            lastInvoiceByMeter.TryGetValue(meter.Id, out var lastInvoice);

            var ownerId = Guid.Empty;
            var ownerName = "—";
            if (property is not null)
            {
                ownerId = property.OwnerId.Value;
                ownerName = await ResolveOwnerNameAsync(property.OwnerId, ownerNameCache, ct);
            }

            results.Add(new MeterNetworkStatusDto(
                meter.Id.Value,
                meter.SerialNumber,
                (int)meter.ConnectionType,
                (int)meter.PairingStatus,
                status is not null && status.IsOnline(now),
                status?.LastHeartbeatUtc,
                property?.Id.Value ?? Guid.Empty,
                property?.Name ?? "—",
                ownerId,
                ownerName,
                status?.MonthConsumptionKwh ?? 0,
                lastInvoice is null ? null : (int)lastInvoice.Status,
                lastInvoice?.IssuedAtUtc));
        }

        // Offline/error meters surface first — that's what an admin scanning the grid cares about.
        return results
            .OrderBy(r => r.IsOnline)
            .ThenBy(r => r.SerialNumber)
            .ToList();
    }

    public async Task<IReadOnlyList<PaymentRecordDto>> GetPaymentsAsync(int take, CancellationToken ct = default)
    {
        var paid = (await _invoices.GetAllAsync(ct))
            .Where(i => i.Status == InvoiceStatus.Paid)
            .OrderByDescending(i => i.PaidAtUtc)
            .Take(Math.Clamp(take, 1, 500))
            .ToList();

        var ownerNameCache = new Dictionary<EntityId, string>();
        var results = new List<PaymentRecordDto>(paid.Count);
        foreach (var invoice in paid)
        {
            var consumerName = await ResolveOwnerNameAsync(invoice.ConsumerId, ownerNameCache, ct);
            results.Add(new PaymentRecordDto(
                invoice.Id.Value,
                invoice.SerialNumber,
                invoice.ConsumerId.Value,
                consumerName,
                invoice.Year,
                invoice.Month,
                invoice.TotalAmountRsd,
                invoice.IssuedAtUtc,
                invoice.PaidAtUtc));
        }

        return results;
    }

    public async Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync(CancellationToken ct = default)
    {
        var invoices = await _invoices.GetAllAsync(ct);
        var paid = invoices.Where(i => i.Status == InvoiceStatus.Paid).ToList();
        var unpaid = invoices.Where(i => i.Status == InvoiceStatus.Unpaid).ToList();

        return new InvoiceStatisticsDto(
            invoices.Count,
            paid.Count,
            unpaid.Count,
            invoices.Count(i => i.EmailSent),
            invoices.Count(i => !i.EmailSent),
            paid.Sum(i => i.TotalAmountRsd),
            unpaid.Sum(i => i.TotalAmountRsd));
    }

    public async Task<IReadOnlyList<AlertLogDto>> GetRecentAlertsAsync(int take, CancellationToken ct = default)
    {
        var alerts = await _alerts.GetRecentAsync(Math.Clamp(take, 1, 500), ct);
        return alerts
            .Select(a => new AlertLogDto(
                a.Id, (int)a.Type, (int)a.Severity, (int)a.Audience,
                a.MeterId.Value, a.SerialNumber, a.Message, a.OccurredAtUtc, a.EmailSent))
            .ToList();
    }

    private async Task<string> ResolveOwnerNameAsync(EntityId userId, Dictionary<EntityId, string> cache, CancellationToken ct)
    {
        if (cache.TryGetValue(userId, out var cached))
        {
            return cached;
        }

        var user = await _users.GetByIdAsync(userId, ct);
        var name = user?.FullName ?? "—";
        cache[userId] = name;
        return name;
    }
}
