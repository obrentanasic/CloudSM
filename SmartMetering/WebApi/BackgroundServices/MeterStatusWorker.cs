using Microsoft.AspNetCore.SignalR;
using SmartMetering.Application.Abstractions;
using SmartMetering.WebApi.Hubs;

namespace SmartMetering.WebApi.BackgroundServices;

/// <summary>
/// Continuously drains the meter-status queue and broadcasts each live update to the SignalR group
/// for its property. High-load mode (no delay while messages keep coming) + idle mode (back off when empty).
/// </summary>
internal sealed class MeterStatusWorker : BackgroundService
{
    private static readonly TimeSpan IdleDelay = TimeSpan.FromSeconds(2);

    private readonly IMeterStatusQueue _queue;
    private readonly IHubContext<TelemetryHub> _hub;
    private readonly ILogger<MeterStatusWorker> _logger;

    public MeterStatusWorker(
        IMeterStatusQueue queue,
        IHubContext<TelemetryHub> hub,
        ILogger<MeterStatusWorker> logger)
    {
        _queue = queue;
        _hub = hub;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("[WORKER] Meter status worker started listening.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var foundMessage = false;
            try
            {
                var message = await _queue.ReceiveAsync(stoppingToken);
                if (message is not null)
                {
                    foundMessage = true;
                    var update = message.Body;

                    await _hub.Clients
                        .Group(update.PropertyId.ToString())
                        .SendAsync("ReceiveMeterUpdate", update, stoppingToken);

                    await _queue.CompleteAsync(message, stoppingToken);
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[WORKER] Error processing meter status queue.");
            }

            if (!foundMessage)
            {
                await Task.Delay(IdleDelay, stoppingToken);
            }
        }
    }
}
