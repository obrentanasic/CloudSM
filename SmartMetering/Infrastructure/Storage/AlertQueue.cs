using Azure.Storage.Queues;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Alerts;
using SmartMetering.Application.Common;

namespace SmartMetering.Infrastructure.Storage;

public sealed class AlertQueue : IAlertQueue
{
    private readonly QueueClient _queue;
    private readonly IJsonSerializer _serializer;

    public AlertQueue(StorageOptions options, IJsonSerializer serializer)
    {
        _queue = new QueueClient(
            options.ConnectionString,
            StorageOptions.AlertQueue,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        _queue.CreateIfNotExists();
        _serializer = serializer;
    }

    public async Task EnqueueAsync(AlertMessage message, CancellationToken ct = default)
    {
        await _queue.SendMessageAsync(_serializer.Serialize(message), ct);
    }
}
