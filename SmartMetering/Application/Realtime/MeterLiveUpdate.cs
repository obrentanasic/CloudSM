namespace SmartMetering.Application.Realtime;

/// <summary>Payload pushed to dashboard clients (via SignalR) for one fresh measurement.</summary>
public sealed class MeterLiveUpdate
{
    public Guid PropertyId { get; set; }

    public Guid MeterId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public int ConnectionType { get; set; }

    public double TotalEnergyKwh { get; set; }

    public double CurrentLoadKw { get; set; }

    public double? Voltage { get; set; }

    public int Tariff { get; set; }

    public DateTime ObservationTime { get; set; }
}
