using SmartMetering.Domain.Common;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Application.Abstractions;

public interface ITelemetryRepository
{
    Task SaveAsync(Telemetry telemetry, CancellationToken ct = default);

    Task<IReadOnlyList<Telemetry>> GetRecentAsync(EntityId meterId, int take, CancellationToken ct = default);

    Task<IReadOnlyList<Telemetry>> GetForPeriodAsync(EntityId meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    Task<Telemetry?> GetPreviousBeforeAsync(EntityId meterId, DateTime beforeUtc, CancellationToken ct = default);
}
