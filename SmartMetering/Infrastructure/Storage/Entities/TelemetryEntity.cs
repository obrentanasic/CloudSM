using SmartMetering.Infrastructure.Common;

namespace SmartMetering.Infrastructure.Storage.Entities;

/// <summary>
/// Table Storage row for a measurement. PartitionKey = MeterId, RowKey = reverse-ticks + "_" + id
/// so the newest measurement for a meter is always first (O(1) "latest").
/// </summary>
public sealed class TelemetryEntity : BaseTableEntity
{
    public string SerialNumber { get; set; } = string.Empty;

    public int ConnectionType { get; set; }

    public double TotalEnergyKwh { get; set; }

    public double CurrentLoadKw { get; set; }

    public double? VoltageL1 { get; set; }

    public double? VoltageL2 { get; set; }

    public double? VoltageL3 { get; set; }

    public double? CurrentL1 { get; set; }

    public double? CurrentL2 { get; set; }

    public double? CurrentL3 { get; set; }

    public double? PowerFactorL1 { get; set; }

    public double? PowerFactorL2 { get; set; }

    public double? PowerFactorL3 { get; set; }

    public DateTimeOffset ObservationTime { get; set; }

    public int Tariff { get; set; }
}
