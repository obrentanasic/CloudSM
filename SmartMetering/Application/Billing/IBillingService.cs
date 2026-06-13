using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Billing;

public interface IBillingService
{
    Task<IReadOnlyList<TariffModelDto>> GetTariffModelsAsync(CancellationToken ct = default);

    Task<TariffModelDto?> GetActiveTariffAsync(CancellationToken ct = default);

    Task<Guid> CreateTariffModelAsync(CreateTariffModelRequest request, bool activate, CancellationToken ct = default);

    Task ActivateTariffModelAsync(Guid tariffId, CancellationToken ct = default);

    Task<GeneratedInvoicesDto> GenerateMonthlyInvoicesAsync(int year, int month, CancellationToken ct = default);

    Task<InvoicePageDto> GetPropertyInvoicesAsync(EntityId ownerId, Guid propertyId, int page, int pageSize, CancellationToken ct = default);

    Task<InvoiceFileDto> GetInvoicePdfAsync(EntityId ownerId, Guid invoiceId, CancellationToken ct = default);
}
