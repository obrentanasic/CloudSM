using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class SmartMeterRepository : ISmartMeterRepository
{
    private readonly AppDbContext _db;

    public SmartMeterRepository(AppDbContext db) => _db = db;

    public Task<SmartMeter?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        _db.SmartMeters.FirstOrDefaultAsync(m => m.Id == id, ct);

    public Task<SmartMeter?> GetBySerialAsync(string serialNumber, CancellationToken ct = default)
    {
        var serial = serialNumber.Trim().ToUpper();
        return _db.SmartMeters.FirstOrDefaultAsync(m => m.SerialNumber == serial, ct);
    }

    public Task<SmartMeter?> GetByDeviceTokenAsync(string deviceAccessToken, CancellationToken ct = default) =>
        _db.SmartMeters.FirstOrDefaultAsync(m => m.DeviceAccessToken == deviceAccessToken, ct);

    public async Task<IReadOnlyList<SmartMeter>> GetByPropertyAsync(EntityId propertyId, CancellationToken ct = default) =>
        await _db.SmartMeters
            .Where(m => m.PropertyId == propertyId)
            .OrderByDescending(m => m.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SmartMeter>> GetByOwnerAsync(EntityId ownerId, CancellationToken ct = default) =>
        await _db.SmartMeters
            .Where(m => _db.Properties.Any(p => p.Id == m.PropertyId && p.OwnerId == ownerId))
            .ToListAsync(ct);

    public async Task<IReadOnlyList<SmartMeter>> GetPairedAsync(CancellationToken ct = default) =>
        await _db.SmartMeters
            .Where(m => m.PairingStatus == PairingStatus.Paired)
            .OrderBy(m => m.SerialNumber)
            .ToListAsync(ct);

    public Task<bool> SerialExistsAsync(string serialNumber, CancellationToken ct = default)
    {
        var serial = serialNumber.Trim().ToUpper();
        return _db.SmartMeters.AnyAsync(m => m.SerialNumber == serial, ct);
    }

    public Task<bool> AnyForPropertyAsync(EntityId propertyId, CancellationToken ct = default) =>
        _db.SmartMeters.AnyAsync(m => m.PropertyId == propertyId, ct);

    public async Task AddAsync(SmartMeter meter, CancellationToken ct = default) =>
        await _db.SmartMeters.AddAsync(meter, ct);

    public void Remove(SmartMeter meter) => _db.SmartMeters.Remove(meter);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
