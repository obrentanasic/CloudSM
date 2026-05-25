using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Domain.Metering;

/// <summary>Latest-known snapshot of a meter (persisted in Table Storage, overwritten each cycle).</summary>
public sealed class MeterStatus
{
    public static readonly TimeSpan OfflineThreshold = TimeSpan.FromMinutes(2);

    private MeterStatus()
    {
        SerialNumber = string.Empty;
    }

    public EntityId MeterId { get; private set; }

    public string SerialNumber { get; private set; }

    public ConnectionType ConnectionType { get; private set; }

    public double LastTotalEnergyKwh { get; private set; }

    public double LastLoadKw { get; private set; }

    public double? LastVoltage { get; private set; }

    public TariffPeriod CurrentTariff { get; private set; }

    public DateTime LastHeartbeatUtc { get; private set; }

    public static MeterStatus FromTelemetry(Telemetry t) => new()
    {
        MeterId = t.MeterId,
        SerialNumber = t.SerialNumber,
        ConnectionType = t.ConnectionType,
        LastTotalEnergyKwh = t.TotalEnergyKwh,
        LastLoadKw = t.CurrentLoadKw,
        LastVoltage = t.RepresentativeVoltage,
        CurrentTariff = t.Tariff,
        LastHeartbeatUtc = DateTime.UtcNow,
    };

    public static MeterStatus Rehydrate(
        EntityId meterId, string serialNumber, ConnectionType connectionType,
        double lastTotalEnergyKwh, double lastLoadKw, double? lastVoltage,
        TariffPeriod currentTariff, DateTime lastHeartbeatUtc) => new()
    {
        MeterId = meterId,
        SerialNumber = serialNumber,
        ConnectionType = connectionType,
        LastTotalEnergyKwh = lastTotalEnergyKwh,
        LastLoadKw = lastLoadKw,
        LastVoltage = lastVoltage,
        CurrentTariff = currentTariff,
        LastHeartbeatUtc = lastHeartbeatUtc,
    };

    public bool IsOnline(DateTime utcNow) => utcNow - LastHeartbeatUtc < OfflineThreshold;
}
