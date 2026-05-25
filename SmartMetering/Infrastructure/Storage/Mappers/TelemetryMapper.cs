using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;
using SmartMetering.Infrastructure.Storage.Entities;

namespace SmartMetering.Infrastructure.Storage.Mappers;

public static class TelemetryMapper
{
    public static TelemetryEntity ToEntity(Telemetry t)
    {
        // Reverse-ticks prefix keeps the newest measurement at the top of the partition.
        var reverseTicks = (DateTime.MaxValue.Ticks - t.ObservationTime.Ticks).ToString("d19");

        return new TelemetryEntity
        {
            PartitionKey = t.MeterId.ToString(),
            RowKey = $"{reverseTicks}_{t.Id}",
            SerialNumber = t.SerialNumber,
            ConnectionType = (int)t.ConnectionType,
            TotalEnergyKwh = t.TotalEnergyKwh,
            CurrentLoadKw = t.CurrentLoadKw,
            VoltageL1 = t.VoltageL1,
            VoltageL2 = t.VoltageL2,
            VoltageL3 = t.VoltageL3,
            CurrentL1 = t.CurrentL1,
            CurrentL2 = t.CurrentL2,
            CurrentL3 = t.CurrentL3,
            PowerFactorL1 = t.PowerFactorL1,
            PowerFactorL2 = t.PowerFactorL2,
            PowerFactorL3 = t.PowerFactorL3,
            ObservationTime = new DateTimeOffset(DateTime.SpecifyKind(t.ObservationTime, DateTimeKind.Utc)),
            Tariff = (int)t.Tariff,
        };
    }

    public static Telemetry ToDomain(TelemetryEntity e) =>
        Telemetry.Create(
            EntityId.Parse(e.PartitionKey),
            e.SerialNumber,
            (ConnectionType)e.ConnectionType,
            e.TotalEnergyKwh,
            e.CurrentLoadKw,
            e.VoltageL1, e.VoltageL2, e.VoltageL3,
            e.CurrentL1, e.CurrentL2, e.CurrentL3,
            e.PowerFactorL1, e.PowerFactorL2, e.PowerFactorL3,
            e.ObservationTime.UtcDateTime);
}
