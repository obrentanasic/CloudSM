using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Analytics;

public interface IAnalyticsService
{
    Task<IReadOnlyList<TelemetryPointDto>> GetRecentTelemetryAsync(EntityId ownerId, Guid meterId, int take, CancellationToken ct = default);

    Task<TelemetryHistoryDto> GetTelemetryHistoryAsync(
        EntityId ownerId,
        Guid meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int take,
        CancellationToken ct = default);

    Task<MeterStatusDto?> GetMeterStatusAsync(EntityId ownerId, Guid meterId, CancellationToken ct = default);

    Task<IReadOnlyList<MeterStatusDto>> GetPropertyLiveAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default);
}
