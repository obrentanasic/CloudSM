using SmartMetering.Domain.Common;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;
using SmartMetering.Infrastructure.Storage.Entities;

namespace SmartMetering.Infrastructure.Storage.Mappers;

public static class MeterStatusMapper
{
    public static MeterStatusEntity ToEntity(MeterStatus s) => new()
    {
        PartitionKey = MeterStatusEntity.Partition,
        RowKey = s.MeterId.ToString(),
        SerialNumber = s.SerialNumber,
        ConnectionType = (int)s.ConnectionType,
        LastTotalEnergyKwh = s.LastTotalEnergyKwh,
        LastLoadKw = s.LastLoadKw,
        LastVoltage = s.LastVoltage,
        CurrentTariff = (int)s.CurrentTariff,
        LastHeartbeatUtc = new DateTimeOffset(DateTime.SpecifyKind(s.LastHeartbeatUtc, DateTimeKind.Utc)),
    };

    public static MeterStatus ToDomain(MeterStatusEntity e) =>
        MeterStatus.Rehydrate(
            EntityId.Parse(e.RowKey),
            e.SerialNumber,
            (ConnectionType)e.ConnectionType,
            e.LastTotalEnergyKwh,
            e.LastLoadKw,
            e.LastVoltage,
            (TariffPeriod)e.CurrentTariff,
            e.LastHeartbeatUtc.UtcDateTime);
}
