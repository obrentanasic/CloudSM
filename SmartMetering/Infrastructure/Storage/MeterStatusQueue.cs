using Azure.Storage.Queues;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Application.Realtime;

namespace SmartMetering.Infrastructure.Storage;

public sealed class MeterStatusQueue : IMeterStatusQueue
{
    private readonly QueueClient _queue;
    private readonly IJsonSerializer _serializer;

    public MeterStatusQueue(StorageOptions options, IJsonSerializer serializer)
    {
        _queue = new QueueClient(
            options.ConnectionString,
            StorageOptions.MeterStatusQueue,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        _queue.CreateIfNotExists();
        _serializer = serializer;
    }

    public async Task EnqueueAsync(MeterLiveUpdate update, CancellationToken ct = default)
    {
        await _queue.SendMessageAsync(_serializer.Serialize(update), ct);
    }

    public async Task<MeterStatusQueueMessage?> ReceiveAsync(CancellationToken ct = default)
    {
        var response = await _queue.ReceiveMessageAsync(TimeSpan.FromSeconds(30), ct);
        var message = response.Value;
        if (message is null)
        {
            return null;
        }

        var body = _serializer.Deserialize<MeterLiveUpdate>(message.MessageText);
        if (body is null)
        {
            // Unreadable message: drop it so it doesn't loop forever.
            await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
            return null;
        }

        return new MeterStatusQueueMessage
        {
            Body = body,
            MessageId = message.MessageId,
            PopReceipt = message.PopReceipt,
        };
    }

    public async Task CompleteAsync(MeterStatusQueueMessage message, CancellationToken ct = default)
    {
        await _queue.DeleteMessageAsync(message.MessageId, message.PopReceipt, ct);
    }
}
