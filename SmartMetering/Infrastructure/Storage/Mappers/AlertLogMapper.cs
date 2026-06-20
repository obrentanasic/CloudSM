using SmartMetering.Domain.Alerts;
using SmartMetering.Domain.Common;
using SmartMetering.Infrastructure.Storage.Entities;

namespace SmartMetering.Infrastructure.Storage.Mappers;

public static class AlertLogMapper
{
    public static AlertLogEntity ToEntity(AlertLogEntry entry) => new()
    {
        PartitionKey = AlertLogEntity.Partition,
        RowKey = AlertLogEntity.BuildRowKey(entry.OccurredAtUtc, entry.Id),
        Id = entry.Id,
        Type = (int)entry.Type,
        Severity = (int)entry.Severity,
        Audience = (int)entry.Audience,
        MeterId = entry.MeterId.Value,
        SerialNumber = entry.SerialNumber,
        Message = entry.Message,
        OccurredAtUtc = new DateTimeOffset(DateTime.SpecifyKind(entry.OccurredAtUtc, DateTimeKind.Utc)),
        EmailSent = entry.EmailSent,
    };

    public static AlertLogEntry ToDomain(AlertLogEntity e) => AlertLogEntry.Rehydrate(
        e.Id,
        (AlertType)e.Type,
        (AlertSeverity)e.Severity,
        (AlertAudience)e.Audience,
        EntityId.From(e.MeterId),
        e.SerialNumber,
        e.Message,
        e.OccurredAtUtc.UtcDateTime,
        e.EmailSent);
}
