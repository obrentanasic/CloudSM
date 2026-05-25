using System.Text.RegularExpressions;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Application.Meters;

public sealed partial class MeterService : IMeterService
{
    private readonly ISmartMeterRepository _meters;
    private readonly IPropertyRepository _properties;

    public MeterService(ISmartMeterRepository meters, IPropertyRepository properties)
    {
        _meters = meters;
        _properties = properties;
    }

    public async Task<Guid> RegisterAsync(EntityId ownerId, RegisterMeterRequest request, CancellationToken ct = default)
    {
        await EnsureOwnsPropertyAsync(ownerId, request.PropertyId, ct);

        var serial = request.SerialNumber.Trim().ToUpperInvariant();
        if (!SerialNumberRegex().IsMatch(serial))
        {
            throw new AppException("Серијски број мора бити у формату SM-YYYY-XXXXX.");
        }

        if (await _meters.SerialExistsAsync(serial, ct))
        {
            throw new ConflictException($"Бројило са серијским бројем '{serial}' већ постоји.");
        }

        var meter = SmartMeter.Register(serial, request.ConnectionType, request.Note, EntityId.From(request.PropertyId));
        await _meters.AddAsync(meter, ct);
        await _meters.SaveChangesAsync(ct);
        return meter.Id.Value;
    }

    public async Task<IReadOnlyList<MeterDto>> GetByPropertyAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default)
    {
        await EnsureOwnsPropertyAsync(ownerId, propertyId, ct);
        var meters = await _meters.GetByPropertyAsync(EntityId.From(propertyId), ct);
        return meters.Select(Map).ToList();
    }

    public async Task UpdateAsync(EntityId ownerId, Guid meterId, UpdateMeterRequest request, CancellationToken ct = default)
    {
        var meter = await GetOwnedMeterAsync(ownerId, meterId, ct);
        meter.UpdateDetails(request.ConnectionType, request.Note);
        await _meters.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EntityId ownerId, Guid meterId, CancellationToken ct = default)
    {
        var meter = await GetOwnedMeterAsync(ownerId, meterId, ct);
        _meters.Remove(meter);
        await _meters.SaveChangesAsync(ct);
    }

    private async Task<SmartMeter> GetOwnedMeterAsync(EntityId ownerId, Guid meterId, CancellationToken ct)
    {
        var meter = await _meters.GetByIdAsync(EntityId.From(meterId), ct)
            ?? throw new NotFoundException("Бројило није пронађено.");

        await EnsureOwnsPropertyAsync(ownerId, meter.PropertyId.Value, ct);
        return meter;
    }

    private async Task EnsureOwnsPropertyAsync(EntityId ownerId, Guid propertyId, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(EntityId.From(propertyId), ct);
        if (property is null || !property.IsOwnedBy(ownerId))
        {
            throw new NotFoundException("Објекат није пронађен.");
        }
    }

    private static MeterDto Map(SmartMeter m) =>
        new(m.Id.Value, m.PropertyId.Value, m.SerialNumber, m.ConnectionType, m.MaxApprovedPowerKw, m.Note, m.PairingStatus);

    [GeneratedRegex(@"^SM-\d{4}-\d{5}$")]
    private static partial Regex SerialNumberRegex();
}
