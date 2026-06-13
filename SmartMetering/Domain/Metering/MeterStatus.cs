using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Domain.Metering;

/// <summary>Latest-known snapshot of a meter (Table Storage). Also carries alert-dedup flags and the monthly baseline.</summary>
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

    public bool VoltageAlerted { get; private set; }

    public bool LoadAlerted { get; private set; }

    public bool OfflineAlerted { get; private set; }

    public double MonthBaselineKwh { get; private set; }

    public string? BaselineMonth { get; private set; }

    public double MonthHighTariffKwh { get; private set; }

    public double MonthLowTariffKwh { get; private set; }

    public double MonthConsumptionKwh => Math.Max(0, LastTotalEnergyKwh - MonthBaselineKwh);

    public bool IsOnline(DateTime utcNow) => utcNow - LastHeartbeatUtc < OfflineThreshold;

    public static MeterStatus CreateNew(EntityId meterId, string serialNumber, ConnectionType connectionType) => new()
    {
        MeterId = meterId,
        SerialNumber = serialNumber,
        ConnectionType = connectionType,
    };

    public static MeterStatus Rehydrate(
        EntityId meterId, string serialNumber, ConnectionType connectionType,
        double lastTotalEnergyKwh, double lastLoadKw, double? lastVoltage,
        TariffPeriod currentTariff, DateTime lastHeartbeatUtc,
        bool voltageAlerted, bool loadAlerted, bool offlineAlerted,
        double monthBaselineKwh, string? baselineMonth,
        double monthHighTariffKwh, double monthLowTariffKwh) => new()
    {
        MeterId = meterId,
        SerialNumber = serialNumber,
        ConnectionType = connectionType,
        LastTotalEnergyKwh = lastTotalEnergyKwh,
        LastLoadKw = lastLoadKw,
        LastVoltage = lastVoltage,
        CurrentTariff = currentTariff,
        LastHeartbeatUtc = lastHeartbeatUtc,
        VoltageAlerted = voltageAlerted,
        LoadAlerted = loadAlerted,
        OfflineAlerted = offlineAlerted,
        MonthBaselineKwh = monthBaselineKwh,
        BaselineMonth = baselineMonth,
        MonthHighTariffKwh = monthHighTariffKwh,
        MonthLowTariffKwh = monthLowTariffKwh,
    };

    public void ApplyTelemetry(Telemetry t)
    {
        var previousTotal = LastTotalEnergyKwh;
        var previousBaselineMonth = BaselineMonth;
        SerialNumber = t.SerialNumber;
        ConnectionType = t.ConnectionType;
        LastTotalEnergyKwh = t.TotalEnergyKwh;
        LastLoadKw = t.CurrentLoadKw;
        LastVoltage = t.RepresentativeVoltage;
        CurrentTariff = t.Tariff;
        LastHeartbeatUtc = DateTime.UtcNow;

        var month = t.ObservationTime.ToString("yyyy-MM");
        if (BaselineMonth != month)
        {
            BaselineMonth = month;
            MonthBaselineKwh = previousBaselineMonth is null ? t.TotalEnergyKwh : previousTotal;
            MonthHighTariffKwh = 0;
            MonthLowTariffKwh = 0;
        }

        if (previousBaselineMonth is not null)
        {
            var delta = Math.Max(0, t.TotalEnergyKwh - previousTotal);
            if (t.Tariff == TariffPeriod.High)
            {
                MonthHighTariffKwh += delta;
            }
            else
            {
                MonthLowTariffKwh += delta;
            }
        }

        // Receiving data means the device is back online.
        OfflineAlerted = false;
    }

    /// <summary>Marks the voltage alert and returns true only on the first transition (alert once).</summary>
    public bool FlagVoltageAlert()
    {
        if (VoltageAlerted)
        {
            return false;
        }

        VoltageAlerted = true;
        return true;
    }

    public void ClearVoltageAlert() => VoltageAlerted = false;

    public bool FlagLoadAlert()
    {
        if (LoadAlerted)
        {
            return false;
        }

        LoadAlerted = true;
        return true;
    }

    public void ClearLoadAlert() => LoadAlerted = false;

    public bool FlagOfflineAlert()
    {
        if (OfflineAlerted)
        {
            return false;
        }

        OfflineAlerted = true;
        return true;
    }
}
