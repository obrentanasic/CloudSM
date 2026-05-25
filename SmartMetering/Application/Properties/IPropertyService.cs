using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Properties;

public interface IPropertyService
{
    Task<Guid> CreateAsync(EntityId ownerId, CreatePropertyRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<PropertyDto>> GetMineAsync(EntityId ownerId, CancellationToken ct = default);

    Task<PropertyDto> GetByIdAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default);

    Task UpdateAsync(EntityId ownerId, Guid propertyId, UpdatePropertyRequest request, CancellationToken ct = default);

    Task DeleteAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default);
}
