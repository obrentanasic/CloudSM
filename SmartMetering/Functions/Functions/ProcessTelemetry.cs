using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Ingestion;
using SmartMetering.Application.Realtime;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Queue-triggered processor. Persists the measurement history first (priority persistence), then
/// updates the meter's current-state snapshot (eventual consistency — a failed status write is
/// corrected by the next cycle).
/// </summary>
public sealed class ProcessTelemetry
{
    private readonly ITelemetryRepository _telemetry;
    private readonly IMeterStatusRepository _status;
    private readonly IMeterStatusQueue _liveUpdates;
    private readonly ILogger<ProcessTelemetry> _logger;

    public ProcessTelemetry(
        ITelemetryRepository telemetry,
        IMeterStatusRepository status,
        IMeterStatusQueue liveUpdates,
        ILogger<ProcessTelemetry> logger)
    {
        _telemetry = telemetry;
        _status = status;
        _liveUpdates = liveUpdates;
        _logger = logger;
    }

    [Function("ProcessTelemetry")]
    public async Task Run(
        [QueueTrigger(StorageQueues.Telemetry, Connection = "StorageConnectionString")] TelemetryQueueMessage message,
        CancellationToken ct)
    {
        var telemetry = Telemetry.Create(
            EntityId.From(message.MeterId),
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

        // 2. Eventual consistency: snapshot update may fail and be corrected next cycle.
        try
        {
            await _status.SaveAsync(MeterStatus.FromTelemetry(telemetry), ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Status snapshot update failed for meter {Serial}; will self-heal next cycle.", message.SerialNumber);
        }

        // 3. Push a live update for the dashboard (best-effort).
        try
        {
            await _liveUpdates.EnqueueAsync(new MeterLiveUpdate
            {
                PropertyId = message.PropertyId,
                MeterId = message.MeterId,
                SerialNumber = telemetry.SerialNumber,
                ConnectionType = (int)telemetry.ConnectionType,
                TotalEnergyKwh = telemetry.TotalEnergyKwh,
                CurrentLoadKw = telemetry.CurrentLoadKw,
                Voltage = telemetry.RepresentativeVoltage,
                Tariff = (int)telemetry.Tariff,
                ObservationTime = telemetry.ObservationTime,
            }, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Live update enqueue failed for meter {Serial}.", message.SerialNumber);
        }

        _logger.LogInformation("Processed telemetry for {Serial} ({Tariff}).", telemetry.SerialNumber, telemetry.Tariff);
    }
}

internal static class StorageQueues
{
    public const string Telemetry = "telemetry-queue";
}
