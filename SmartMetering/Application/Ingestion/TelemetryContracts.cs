namespace SmartMetering.Application.Ingestion;

/// <summary>Handshake request sent by the device simulator to pair itself.</summary>
public sealed record RegisterDeviceRequest(string SerialNumber, string DeviceUuid);

public sealed record RegisterDeviceResponse(string DeviceAccessToken);

/// <summary>Raw measurement payload posted by a paired device to the ingestion endpoint.</summary>
public sealed class TelemetryIngestRequest
{
    public double TotalEnergyKwh { get; set; }

    public double CurrentLoadKw { get; set; }

    public DateTime ObservationTime { get; set; }

    public double? VoltageL1 { get; set; }

    public double? VoltageL2 { get; set; }

    public double? VoltageL3 { get; set; }

    public double? CurrentL1 { get; set; }

    public double? CurrentL2 { get; set; }

    public double? CurrentL3 { get; set; }

    public double? PowerFactorL1 { get; set; }

    public double? PowerFactorL2 { get; set; }

    public double? PowerFactorL3 { get; set; }
}

/// <summary>Message placed on the telemetry queue once the device has been authenticated.</summary>
public sealed class TelemetryQueueMessage
{
    public Guid MeterId { get; set; }

    public Guid PropertyId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public int ConnectionType { get; set; }

    public double TotalEnergyKwh { get; set; }

    public double CurrentLoadKw { get; set; }

    public DateTime ObservationTime { get; set; }

    public double? VoltageL1 { get; set; }

    public double? VoltageL2 { get; set; }

    public double? VoltageL3 { get; set; }

    public double? CurrentL1 { get; set; }

    public double? CurrentL2 { get; set; }

    public double? CurrentL3 { get; set; }

    public double? PowerFactorL1 { get; set; }

    public double? PowerFactorL2 { get; set; }

    public double? PowerFactorL3 { get; set; }
}
