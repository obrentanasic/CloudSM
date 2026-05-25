using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Meters;

public interface IMeterService
{
    Task<Guid> RegisterAsync(EntityId ownerId, RegisterMeterRequest request, CancellationToken ct = default);

    Task<IReadOnlyList<MeterDto>> GetByPropertyAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default);

    Task UpdateAsync(EntityId ownerId, Guid meterId, UpdateMeterRequest request, CancellationToken ct = default);

    Task DeleteAsync(EntityId ownerId, Guid meterId, CancellationToken ct = default);
}
