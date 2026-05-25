using SmartMetering.Domain.Common;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Application.Abstractions;

public interface IPropertyRepository
{
    Task<Property?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<IReadOnlyList<Property>> GetByOwnerAsync(EntityId ownerId, CancellationToken ct = default);

    Task AddAsync(Property property, CancellationToken ct = default);

    void Remove(Property property);

    Task SaveChangesAsync(CancellationToken ct = default);
}
