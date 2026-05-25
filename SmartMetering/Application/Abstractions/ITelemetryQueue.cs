using SmartMetering.Application.Ingestion;

namespace SmartMetering.Application.Abstractions;

public interface ITelemetryQueue
{
    Task EnqueueAsync(TelemetryQueueMessage message, CancellationToken ct = default);
}
