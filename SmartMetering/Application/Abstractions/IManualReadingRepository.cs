using SmartMetering.Domain.Common;
using SmartMetering.Domain.ManualReadings;

namespace SmartMetering.Application.Abstractions;

public interface IManualReadingRepository
{
    Task<ManualReading?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<IReadOnlyList<ManualReading>> GetPendingAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ManualReading>> GetByConsumerAsync(EntityId consumerId, CancellationToken ct = default);

    /// <summary>Approved (Processed) readings for a meter within a billing period — fed into the monthly calculation.</summary>
    Task<IReadOnlyList<ManualReading>> GetProcessedForPeriodAsync(
        EntityId meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default);

    Task AddAsync(ManualReading reading, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
