using SmartMetering.Domain.Common;
using SmartMetering.Domain.ManualReadings;

namespace SmartMetering.Application.Abstractions;

public interface IManualReadingRepository
{
    Task<ManualReading?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    /// <summary>Finds the reading whose original or optimized image is stored at <paramref name="blobName"/>.</summary>
    Task<ManualReading?> GetByBlobNameAsync(string blobName, CancellationToken ct = default);

    Task<IReadOnlyList<ManualReading>> GetPendingAsync(CancellationToken ct = default);

    Task<IReadOnlyList<ManualReading>> GetByConsumerAsync(EntityId consumerId, CancellationToken ct = default);

    Task AddAsync(ManualReading reading, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
