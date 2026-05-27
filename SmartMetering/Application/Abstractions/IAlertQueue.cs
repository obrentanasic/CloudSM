using SmartMetering.Application.Alerts;

namespace SmartMetering.Application.Abstractions;

public interface IAlertQueue
{
    Task EnqueueAsync(AlertMessage message, CancellationToken ct = default);
}
