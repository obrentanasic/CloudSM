using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Alerts;
using SmartMetering.Domain.Alerts;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Timer-triggered grid monitor. Every minute scans meter snapshots and raises a critical alert
/// for any device that has stopped reporting (offline), once per offline episode.
/// </summary>
public sealed class MeterMonitor
{
    private readonly IMeterStatusRepository _status;
    private readonly IAlertQueue _alerts;
    private readonly ILogger<MeterMonitor> _logger;

    public MeterMonitor(IMeterStatusRepository status, IAlertQueue alerts, ILogger<MeterMonitor> logger)
    {
        _status = status;
        _alerts = alerts;
        _logger = logger;
    }

    [Function("MeterMonitor")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo timer, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var statuses = await _status.GetAllAsync(ct);

        foreach (var status in statuses)
        {
            if (status.IsOnline(now) || !status.FlagOfflineAlert())
            {
                continue;
            }

            await _alerts.EnqueueAsync(new AlertMessage
            {
                Type = (int)AlertType.DeviceOffline,
                Severity = (int)AlertSeverity.Critical,
                Audience = (int)AlertAudience.Admin,
                MeterId = status.MeterId.Value,
                SerialNumber = status.SerialNumber,
                Message = $"Бројило {status.SerialNumber} је офлајн од {status.LastHeartbeatUtc:yyyy-MM-dd HH:mm} UTC.",
                OccurredAtUtc = now,
            }, ct);

            await _status.SaveAsync(status, ct);
            _logger.LogWarning("[MONITOR] Offline alert raised for {Serial}.", status.SerialNumber);
        }
    }
}
