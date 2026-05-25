using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Application.Analytics;

public sealed class AnalyticsService : IAnalyticsService
{
    private readonly ISmartMeterRepository _meters;
    private readonly IPropertyRepository _properties;
    private readonly ITelemetryRepository _telemetry;
    private readonly IMeterStatusRepository _status;

    public AnalyticsService(
        ISmartMeterRepository meters,
        IPropertyRepository properties,
        ITelemetryRepository telemetry,
        IMeterStatusRepository status)
    {
        _meters = meters;
        _properties = properties;
        _telemetry = telemetry;
        _status = status;
    }

    public async Task<IReadOnlyList<TelemetryPointDto>> GetRecentTelemetryAsync(EntityId ownerId, Guid meterId, int take, CancellationToken ct = default)
    {
        var meter = await GetOwnedMeterAsync(ownerId, meterId, ct);
        var points = await _telemetry.GetRecentAsync(meter.Id, Math.Clamp(take, 1, 500), ct);
        return points
            .Select(p => new TelemetryPointDto(p.ObservationTime, p.TotalEnergyKwh, p.CurrentLoadKw, p.RepresentativeVoltage, (int)p.Tariff))
            .ToList();
    }

    public async Task<MeterStatusDto?> GetMeterStatusAsync(EntityId ownerId, Guid meterId, CancellationToken ct = default)
    {
        var meter = await GetOwnedMeterAsync(ownerId, meterId, ct);
        var status = await _status.GetByMeterAsync(meter.Id, ct);
        return status is null ? null : Map(status);
    }

    public async Task<IReadOnlyList<MeterStatusDto>> GetPropertyLiveAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default)
    {
        await EnsureOwnsPropertyAsync(ownerId, propertyId, ct);

        var meters = await _meters.GetByPropertyAsync(EntityId.From(propertyId), ct);
        var results = new List<MeterStatusDto>();
        foreach (var meter in meters)
        {
            var status = await _status.GetByMeterAsync(meter.Id, ct);
            if (status is not null)
            {
                results.Add(Map(status));
            }
        }

        return results;
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

    private static MeterStatusDto Map(MeterStatus s) =>
        new(s.MeterId.Value, s.SerialNumber, (int)s.ConnectionType, s.LastTotalEnergyKwh, s.LastLoadKw,
            s.LastVoltage, (int)s.CurrentTariff, s.LastHeartbeatUtc, s.IsOnline(DateTime.UtcNow));
}
