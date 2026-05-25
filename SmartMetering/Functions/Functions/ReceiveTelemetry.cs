using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Application.Ingestion;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Ingestion endpoint. Authenticates the device via the X-Device-Token header, then enqueues the
/// measurement for asynchronous processing (fast accept, decoupled processing).
/// </summary>
public sealed class ReceiveTelemetry
{
    private const string DeviceTokenHeader = "X-Device-Token";

    private readonly ISmartMeterRepository _meters;
    private readonly ITelemetryQueue _queue;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<ReceiveTelemetry> _logger;

    public ReceiveTelemetry(
        ISmartMeterRepository meters,
        ITelemetryQueue queue,
        IJsonSerializer serializer,
        ILogger<ReceiveTelemetry> logger)
    {
        _meters = meters;
        _queue = queue;
        _serializer = serializer;
        _logger = logger;
    }

    [Function("ReceiveTelemetry")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "telemetry")] HttpRequest req,
        CancellationToken ct)
    {
        if (!req.Headers.TryGetValue(DeviceTokenHeader, out var tokenValues) || string.IsNullOrWhiteSpace(tokenValues))
        {
            return new UnauthorizedObjectResult(new { error = "Missing X-Device-Token header." });
        }

        var meter = await _meters.GetByDeviceTokenAsync(tokenValues.ToString(), ct);
        if (meter is null || meter.PairingStatus != PairingStatus.Paired)
        {
            return new UnauthorizedObjectResult(new { error = "Invalid device token." });
        }

        var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
        var request = _serializer.Deserialize<TelemetryIngestRequest>(body);
        if (request is null)
        {
            return new BadRequestObjectResult(new { error = "Invalid telemetry payload." });
        }

        var message = new TelemetryQueueMessage
        {
            MeterId = meter.Id.Value,
            PropertyId = meter.PropertyId.Value,
            SerialNumber = meter.SerialNumber,
            ConnectionType = (int)meter.ConnectionType,
            TotalEnergyKwh = request.TotalEnergyKwh,
            CurrentLoadKw = request.CurrentLoadKw,
            ObservationTime = request.ObservationTime == default ? DateTime.UtcNow : request.ObservationTime,
            VoltageL1 = request.VoltageL1,
            VoltageL2 = request.VoltageL2,
            VoltageL3 = request.VoltageL3,
            CurrentL1 = request.CurrentL1,
            CurrentL2 = request.CurrentL2,
            CurrentL3 = request.CurrentL3,
            PowerFactorL1 = request.PowerFactorL1,
            PowerFactorL2 = request.PowerFactorL2,
            PowerFactorL3 = request.PowerFactorL3,
        };

        await _queue.EnqueueAsync(message, ct);
        _logger.LogInformation("Telemetry queued for meter {Serial}", meter.SerialNumber);

        return new AcceptedResult();
    }
}
