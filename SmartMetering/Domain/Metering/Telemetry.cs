using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Domain.Metering;

/// <summary>
/// A single measurement received from a paired smart meter. Phase-2/3 values are nullable:
/// a single-phase meter populates only L1; a three-phase meter populates L1–L3.
/// </summary>
public sealed class Telemetry : AggregateRoot
{
    private Telemetry()
    {
        SerialNumber = string.Empty;
    }

    private Telemetry(
        EntityId meterId,
        string serialNumber,
        ConnectionType connectionType,
        double totalEnergyKwh,
        double currentLoadKw,
        double? voltageL1, double? voltageL2, double? voltageL3,
        double? currentL1, double? currentL2, double? currentL3,
        double? powerFactorL1, double? powerFactorL2, double? powerFactorL3,
        DateTime observationTime)
    {
        MeterId = meterId;
        SerialNumber = serialNumber;
        ConnectionType = connectionType;
        TotalEnergyKwh = totalEnergyKwh;
        CurrentLoadKw = currentLoadKw;
        VoltageL1 = voltageL1;
        VoltageL2 = voltageL2;
        VoltageL3 = voltageL3;
        CurrentL1 = currentL1;
        CurrentL2 = currentL2;
        CurrentL3 = currentL3;
        PowerFactorL1 = powerFactorL1;
        PowerFactorL2 = powerFactorL2;
        PowerFactorL3 = powerFactorL3;
        ObservationTime = observationTime;
        Tariff = ClassifyTariff(observationTime);
    }

    public EntityId MeterId { get; private set; }

    public string SerialNumber { get; private set; }

    public ConnectionType ConnectionType { get; private set; }

    public double TotalEnergyKwh { get; private set; }

    public double CurrentLoadKw { get; private set; }

    public double? VoltageL1 { get; private set; }

    public double? VoltageL2 { get; private set; }

    public double? VoltageL3 { get; private set; }

    public double? CurrentL1 { get; private set; }

    public double? CurrentL2 { get; private set; }

    public double? CurrentL3 { get; private set; }

    public double? PowerFactorL1 { get; private set; }

    public double? PowerFactorL2 { get; private set; }

    public double? PowerFactorL3 { get; private set; }

    public DateTime ObservationTime { get; private set; }

    public TariffPeriod Tariff { get; private set; }

    /// <summary>Lowest measured phase voltage — used for drop detection and the voltage trend chart.</summary>
    public double? RepresentativeVoltage
    {
        get
        {
            var phases = new[] { VoltageL1, VoltageL2, VoltageL3 }
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToArray();
            return phases.Length == 0 ? null : phases.Min();
        }
    }

    public static TariffPeriod ClassifyTariff(DateTime observationTime)
    {
        var utc = observationTime.Kind switch
        {
            DateTimeKind.Utc => observationTime,
            DateTimeKind.Local => observationTime.ToUniversalTime(),
            _ => DateTime.SpecifyKind(observationTime, DateTimeKind.Utc),
        };
        var tariffTime = TimeZoneInfo.ConvertTimeFromUtc(utc, TariffTimeZone);
        return tariffTime.Hour is >= 7 and < 23 ? TariffPeriod.High : TariffPeriod.Low;
    }

    private static readonly TimeZoneInfo TariffTimeZone = ResolveTariffTimeZone();

    private static TimeZoneInfo ResolveTariffTimeZone()
    {
        foreach (var id in new[] { "Central Europe Standard Time", "Europe/Sarajevo", "Europe/Belgrade" })
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(id);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Local;
    }

    public static Telemetry Create(
        EntityId meterId,
        string serialNumber,
        ConnectionType connectionType,
        double totalEnergyKwh,
        double currentLoadKw,
        double? voltageL1, double? voltageL2, double? voltageL3,
        double? currentL1, double? currentL2, double? currentL3,
        double? powerFactorL1, double? powerFactorL2, double? powerFactorL3,
        DateTime observationTime) =>
        new(meterId, serialNumber, connectionType, totalEnergyKwh, currentLoadKw,
            voltageL1, voltageL2, voltageL3, currentL1, currentL2, currentL3,
            powerFactorL1, powerFactorL2, powerFactorL3, observationTime);
}
