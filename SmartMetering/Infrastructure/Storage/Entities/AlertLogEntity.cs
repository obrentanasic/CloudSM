using SmartMetering.Infrastructure.Common;

namespace SmartMetering.Infrastructure.Storage.Entities;

/// <summary>
/// PartitionKey = constant "AlertLog" (one queryable partition for the admin screen).
/// RowKey is a reverse-chronological prefix (DateTime.MaxValue.Ticks - OccurredAtUtc.Ticks) + the
/// entry id, so a plain ascending query already returns newest-first without an in-memory sort.
/// </summary>
public sealed class AlertLogEntity : BaseTableEntity
{
    public const string Partition = "AlertLog";

    public Guid Id { get; set; }

    public int Type { get; set; }

    public int Severity { get; set; }

    public int Audience { get; set; }

    public Guid MeterId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTimeOffset OccurredAtUtc { get; set; }

    public bool EmailSent { get; set; }

    public static string BuildRowKey(DateTime occurredAtUtc, Guid id) =>
        $"{DateTime.MaxValue.Ticks - occurredAtUtc.Ticks:D19}_{id:N}";
}
