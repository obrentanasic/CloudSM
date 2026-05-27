namespace SmartMetering.Domain.Alerts;

public enum AlertType
{
    VoltageDrop = 0,
    DeviceOffline = 1,
    LoadSpike = 2,
    ConsumptionLimit = 3,
}

public enum AlertSeverity
{
    Warning = 0,
    Critical = 1,
}

public enum AlertAudience
{
    Admin = 0,
    Consumer = 1,
}
