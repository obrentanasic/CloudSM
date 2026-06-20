using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<bool> ExistsAsync(EntityId meterId, int year, int month, CancellationToken ct = default);

    Task<int> CountByPropertyAsync(
        EntityId ownerId,
        EntityId propertyId,
        EntityId? meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default);

    Task<IReadOnlyList<Invoice>> GetByPropertyAsync(
        EntityId ownerId,
        EntityId propertyId,
        EntityId? meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken ct = default);

    /// <summary>All invoices — used for the admin network/payments overview and statistics (Faza 10).</summary>
    Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(Invoice invoice, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
