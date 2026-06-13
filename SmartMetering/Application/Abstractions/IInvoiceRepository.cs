using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Abstractions;

public interface IInvoiceRepository
{
    Task<Invoice?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<bool> ExistsAsync(EntityId meterId, int year, int month, CancellationToken ct = default);

    Task<int> CountByPropertyAsync(EntityId ownerId, EntityId propertyId, CancellationToken ct = default);

    Task<IReadOnlyList<Invoice>> GetByPropertyAsync(EntityId ownerId, EntityId propertyId, int skip, int take, CancellationToken ct = default);

    Task AddAsync(Invoice invoice, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
