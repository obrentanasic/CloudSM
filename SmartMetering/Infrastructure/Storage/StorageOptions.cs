namespace SmartMetering.Infrastructure.Storage;

public sealed class StorageOptions
{
    public StorageOptions(string connectionString) => ConnectionString = connectionString;

    public string ConnectionString { get; }

    public const string TelemetriesTable = "Telemetries";
    public const string MeterStatusesTable = "MeterStatuses";
    public const string TelemetryQueue = "telemetry-queue";
    public const string MeterStatusQueue = "meterstatus-queue";
}
