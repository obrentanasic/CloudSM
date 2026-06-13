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
/// Pairing handshake (steps 2–3): the device sends its UUID + the serial number printed on it.
/// If a registered meter with that serial exists, we bind the UUID, issue a device access token,
/// and return it. The device stores the token and uses it for all telemetry.
/// </summary>
public sealed class RegisterDevice
{
    private readonly ISmartMeterRepository _meters;
    private readonly IDeviceTokenFactory _tokenFactory;
    private readonly IJsonSerializer _serializer;
    private readonly ILogger<RegisterDevice> _logger;

    public RegisterDevice(
        ISmartMeterRepository meters,
        IDeviceTokenFactory tokenFactory,
        IJsonSerializer serializer,
        ILogger<RegisterDevice> logger)
    {
        _meters = meters;
        _tokenFactory = tokenFactory;
        _serializer = serializer;
        _logger = logger;
    }

    [Function("RegisterDevice")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "devices/register")] HttpRequest req,
        CancellationToken ct)
    {
        var body = await new StreamReader(req.Body).ReadToEndAsync(ct);
        var request = _serializer.Deserialize<RegisterDeviceRequest>(body);

        if (request is null || string.IsNullOrWhiteSpace(request.SerialNumber) || string.IsNullOrWhiteSpace(request.DeviceUuid))
        {
            return new BadRequestObjectResult(new { error = "SerialNumber and DeviceUuid are required." });
        }

        var serial = request.SerialNumber.Trim().ToUpperInvariant();
        var deviceUuid = request.DeviceUuid.Trim();
        if (!Guid.TryParse(deviceUuid, out _))
        {
            return new BadRequestObjectResult(new { error = "DeviceUuid must be a valid 128-bit UUID." });
        }

        var meter = await _meters.GetBySerialAsync(serial, ct);
        if (meter is null)
        {
            _logger.LogWarning("Handshake failed: no meter with serial {Serial}", serial);
            return new NotFoundObjectResult(new { error = "No registered meter matches this serial number." });
        }

        if (meter.PairingStatus == PairingStatus.Paired &&
            !string.Equals(meter.DeviceUuid, deviceUuid, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning(
                "Handshake rejected for meter {Serial}: paired UUID {PairedUuid}, attempted UUID {AttemptedUuid}.",
                meter.SerialNumber,
                meter.DeviceUuid,
                deviceUuid);
            return new ConflictObjectResult(new { error = "Meter is already paired with another device." });
        }

        // Reconnects from the same device return the existing token; first pairing issues a new one.
        if (meter.PairingStatus != PairingStatus.Paired || string.IsNullOrEmpty(meter.DeviceAccessToken))
        {
            var token = _tokenFactory.Create();
            meter.CompletePairing(deviceUuid, token);
            await _meters.SaveChangesAsync(ct);
            _logger.LogInformation("Meter {Serial} paired with device {Uuid}", meter.SerialNumber, deviceUuid);
        }

        return new OkObjectResult(new RegisterDeviceResponse(meter.DeviceAccessToken!));
    }
}
