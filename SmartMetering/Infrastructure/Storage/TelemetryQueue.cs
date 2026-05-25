using Azure.Storage.Queues;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Application.Ingestion;

namespace SmartMetering.Infrastructure.Storage;

public sealed class TelemetryQueue : ITelemetryQueue
{
    private readonly QueueClient _queue;
    private readonly IJsonSerializer _serializer;

    public TelemetryQueue(StorageOptions options, IJsonSerializer serializer)
    {
        // Base64 encoding matches the Azure Functions queue-trigger default decoding.
        _queue = new QueueClient(
            options.ConnectionString,
            StorageOptions.TelemetryQueue,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        _queue.CreateIfNotExists();
        _serializer = serializer;
    }

    public async Task EnqueueAsync(TelemetryQueueMessage message, CancellationToken ct = default)
    {
        var json = _serializer.Serialize(message);
        await _queue.SendMessageAsync(json, ct);
    }
}
