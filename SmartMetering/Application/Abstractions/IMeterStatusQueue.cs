using SmartMetering.Application.Realtime;

namespace SmartMetering.Application.Abstractions;

/// <summary>
/// Queue carrying live meter updates from the ingestion functions to the Web API worker,
/// which broadcasts them over SignalR. Uses a pull/receive-complete model (not a trigger).
/// </summary>
public interface IMeterStatusQueue
{
    Task EnqueueAsync(MeterLiveUpdate update, CancellationToken ct = default);

    Task<MeterStatusQueueMessage?> ReceiveAsync(CancellationToken ct = default);

    Task CompleteAsync(MeterStatusQueueMessage message, CancellationToken ct = default);
}

public sealed class MeterStatusQueueMessage
{
    public required MeterLiveUpdate Body { get; init; }

    public required string MessageId { get; init; }

    public required string PopReceipt { get; init; }
}
