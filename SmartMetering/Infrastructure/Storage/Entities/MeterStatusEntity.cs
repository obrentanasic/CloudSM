using SmartMetering.Infrastructure.Common;

namespace SmartMetering.Infrastructure.Storage.Entities;

/// <summary>
/// Latest snapshot per meter. PartitionKey = constant "MeterStatus" (so all statuses are one
/// queryable partition for the admin dashboard), RowKey = MeterId.
/// </summary>
public sealed class MeterStatusEntity : BaseTableEntity
{
    public const string Partition = "MeterStatus";

    public string SerialNumber { get; set; } = string.Empty;

    public int ConnectionType { get; set; }

    public double LastTotalEnergyKwh { get; set; }

    public double LastLoadKw { get; set; }

    public double? LastVoltage { get; set; }

    public int CurrentTariff { get; set; }

    public DateTimeOffset LastHeartbeatUtc { get; set; }

    public bool VoltageAlerted { get; set; }

    public bool LoadAlerted { get; set; }

    public bool OfflineAlerted { get; set; }

    public double MonthBaselineKwh { get; set; }

    public string? BaselineMonth { get; set; }

    public double MonthHighTariffKwh { get; set; }

    public double MonthLowTariffKwh { get; set; }
}
