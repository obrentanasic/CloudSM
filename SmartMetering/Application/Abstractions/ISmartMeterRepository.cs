using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Application.Abstractions;

public interface ISmartMeterRepository
{
    Task<SmartMeter?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<SmartMeter?> GetBySerialAsync(string serialNumber, CancellationToken ct = default);

    Task<SmartMeter?> GetByDeviceTokenAsync(string deviceAccessToken, CancellationToken ct = default);

    Task<IReadOnlyList<SmartMeter>> GetByPropertyAsync(EntityId propertyId, CancellationToken ct = default);

    Task<bool> SerialExistsAsync(string serialNumber, CancellationToken ct = default);

    Task<bool> AnyForPropertyAsync(EntityId propertyId, CancellationToken ct = default);

    Task AddAsync(SmartMeter meter, CancellationToken ct = default);

    void Remove(SmartMeter meter);

    Task SaveChangesAsync(CancellationToken ct = default);
}
