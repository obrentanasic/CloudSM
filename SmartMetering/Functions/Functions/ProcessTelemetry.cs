using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Alerts;
using SmartMetering.Application.Billing;
using SmartMetering.Application.Ingestion;
using SmartMetering.Application.Realtime;
using SmartMetering.Domain.Alerts;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Limits;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Queue-triggered processor. Persists history (priority), updates the snapshot (eventual consistency),
/// evaluates real-time anomaly rules (voltage drop / load spike / consumption limit), and pushes a live update.
/// </summary>
public sealed class ProcessTelemetry
{
    private const double VoltageThresholdVolts = 190;

    private readonly ITelemetryRepository _telemetry;
    private readonly IMeterStatusRepository _status;
    private readonly IMeterStatusQueue _liveUpdates;
    private readonly IAlertQueue _alerts;
    private readonly IPropertyRepository _properties;
    private readonly ISmartMeterRepository _meters;
    private readonly IConsumptionLimitRepository _limits;
    private readonly ITariffModelRepository _tariffs;
    private readonly ILogger<ProcessTelemetry> _logger;

    public ProcessTelemetry(
        ITelemetryRepository telemetry,
        IMeterStatusRepository status,
        IMeterStatusQueue liveUpdates,
        IAlertQueue alerts,
        IPropertyRepository properties,
        ISmartMeterRepository meters,
        IConsumptionLimitRepository limits,
        ITariffModelRepository tariffs,
        ILogger<ProcessTelemetry> logger)
    {
        _telemetry = telemetry;
        _status = status;
        _liveUpdates = liveUpdates;
        _alerts = alerts;
        _properties = properties;
        _meters = meters;
        _limits = limits;
        _tariffs = tariffs;
        _logger = logger;
    }

    [Function("ProcessTelemetry")]
    public async Task Run(
        [QueueTrigger(StorageQueues.Telemetry, Connection = "StorageConnectionString")] TelemetryQueueMessage message,
        CancellationToken ct)
    {
        var meterId = EntityId.From(message.MeterId);
        var telemetry = Telemetry.Create(
            meterId,
            message.SerialNumber,
            (ConnectionType)message.ConnectionType,
            message.TotalEnergyKwh,
            message.CurrentLoadKw,
            message.VoltageL1, message.VoltageL2, message.VoltageL3,
            message.CurrentL1, message.CurrentL2, message.CurrentL3,
            message.PowerFactorL1, message.PowerFactorL2, message.PowerFactorL3,
            message.ObservationTime);

        // 1. Priority persistence: never lose a measurement.
        await _telemetry.SaveAsync(telemetry, ct);

        // 2. Snapshot (carry alert flags + monthly baseline from the previous snapshot).
        var status = await _status.GetByMeterAsync(meterId, ct)
            ?? MeterStatus.CreateNew(meterId, telemetry.SerialNumber, telemetry.ConnectionType);
        status.ApplyTelemetry(telemetry);

        // 3. Real-time anomaly detection (with once-per-episode dedup via status flags).
        await EvaluateVoltageAsync(status, telemetry, ct);
        await EvaluateLoadAsync(status, telemetry, ct);

        try
        {
            await _status.SaveAsync(status, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status snapshot update failed for {Serial}; self-heals next cycle.", message.SerialNumber);
        }

        // 4. Consumption-limit check (best-effort).
        await EvaluateLimitAsync(message, telemetry, ct);

        // 5. Live dashboard update (best-effort).
        await PushLiveUpdateAsync(message, telemetry, ct);

        _logger.LogInformation("Processed telemetry for {Serial} ({Tariff}).", telemetry.SerialNumber, telemetry.Tariff);
    }

    private async Task EvaluateVoltageAsync(MeterStatus status, Telemetry t, CancellationToken ct)
    {
        var voltage = t.RepresentativeVoltage;
        if (voltage is { } v && v < VoltageThresholdVolts)
        {
            if (status.FlagVoltageAlert())
            {
                await _alerts.EnqueueAsync(Alert(AlertType.VoltageDrop, AlertSeverity.Critical, AlertAudience.Admin, t,
                    $"Критичан пад напона: {v:F1} V (бројило {t.SerialNumber})."), ct);
            }
        }
        else
        {
            status.ClearVoltageAlert();
        }
    }

    private async Task EvaluateLoadAsync(MeterStatus status, Telemetry t, CancellationToken ct)
    {
        var maxKw = (double)SmartMeter.PowerFor(t.ConnectionType);
        if (t.CurrentLoadKw > maxKw)
        {
            if (status.FlagLoadAlert())
            {
                await _alerts.EnqueueAsync(Alert(AlertType.LoadSpike, AlertSeverity.Warning, AlertAudience.Admin, t,
                    $"Нагли скок потрошње: {t.CurrentLoadKw:F2} kW (> {maxKw:F2} kW) на бројилу {t.SerialNumber}."), ct);
            }
        }
        else
        {
            status.ClearLoadAlert();
        }
    }

    private async Task EvaluateLimitAsync(TelemetryQueueMessage message, Telemetry t, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(EntityId.From(message.PropertyId), ct);
        if (property is null)
        {
            return;
        }

        var limit = await _limits.GetByUserAsync(property.OwnerId, ct);
        if (limit is null)
        {
            return;
        }

        var month = t.ObservationTime.ToString("yyyy-MM");
        if (!limit.ShouldAlert(month))
        {
            return;
        }

        var meters = await _meters.GetByOwnerAsync(property.OwnerId, ct);
        var exceeded = false;
        string messageText;

        if (limit.Unit == LimitUnit.Kwh)
        {
            double monthTotal = 0;
            foreach (var meter in meters)
            {
                var st = await _status.GetByMeterAsync(meter.Id, ct);
                if (st is not null)
                {
                    monthTotal += st.MonthConsumptionKwh;
                }
            }

            exceeded = monthTotal > (double)limit.Value;
            messageText = $"Prekoracili ste limit potrosnje: {monthTotal:F1} kWh od {limit.Value:F0} kWh u mesecu {month}.";
        }
        else
        {
            var tariff = await _tariffs.GetActiveAsync(ct);
            if (tariff is null)
            {
                return;
            }

            decimal monthTotal = 0;
            foreach (var meter in meters)
            {
                var st = await _status.GetByMeterAsync(meter.Id, ct);
                if (st is null || st.BaselineMonth != month)
                {
                    continue;
                }

                var consumption = new ConsumptionBreakdown(
                    (decimal)Math.Round(st.MonthHighTariffKwh, 3),
                    (decimal)Math.Round(st.MonthLowTariffKwh, 3));
                var breakdown = BillingCalculator.CalculateInvoice(consumption, meter, tariff);
                monthTotal += breakdown.TotalAmountRsd;
            }

            exceeded = monthTotal > limit.Value;
            messageText = $"Prekoracili ste limit potrosnje: {monthTotal:F2} RSD od {limit.Value:F2} RSD u mesecu {month}.";
        }

        if (exceeded)
        {
            await _alerts.EnqueueAsync(new AlertMessage
            {
                Type = (int)AlertType.ConsumptionLimit,
                Severity = (int)AlertSeverity.Warning,
                Audience = (int)AlertAudience.Consumer,
                ConsumerUserId = property.OwnerId.Value,
                MeterId = message.MeterId,
                SerialNumber = message.SerialNumber,
                Message = messageText,
                OccurredAtUtc = DateTime.UtcNow,
            }, ct);

            limit.MarkAlerted(month);
            await _limits.SaveChangesAsync(ct);
        }
    }

    private async Task PushLiveUpdateAsync(TelemetryQueueMessage message, Telemetry t, CancellationToken ct)
    {
        try
        {
            await _liveUpdates.EnqueueAsync(new MeterLiveUpdate
            {
                PropertyId = message.PropertyId,
                MeterId = message.MeterId,
                SerialNumber = t.SerialNumber,
                ConnectionType = (int)t.ConnectionType,
                TotalEnergyKwh = t.TotalEnergyKwh,
                CurrentLoadKw = t.CurrentLoadKw,
                Voltage = t.RepresentativeVoltage,
                Tariff = (int)t.Tariff,
                ObservationTime = t.ObservationTime,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live update enqueue failed for {Serial}.", message.SerialNumber);
        }
    }

    private static AlertMessage Alert(AlertType type, AlertSeverity severity, AlertAudience audience, Telemetry t, string message) =>
        new()
        {
            Type = (int)type,
            Severity = (int)severity,
            Audience = (int)audience,
            MeterId = t.MeterId.Value,
            SerialNumber = t.SerialNumber,
            Message = message,
            OccurredAtUtc = DateTime.UtcNow,
        };
}

internal static class StorageQueues
{
    public const string Telemetry = "telemetry-queue";
    public const string Alerts = "alert-queue";
}
