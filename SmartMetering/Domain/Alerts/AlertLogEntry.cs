using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Alerts;

/// <summary>
/// A persisted record of an alert that was raised and (attempted to be) emailed out.
/// Written by the ProcessAlerts function so Faza 10's admin "pregled upozorenja" screen
/// has something to read — the queue itself is transient and not meant for browsing.
/// </summary>
public sealed class AlertLogEntry
{
    private AlertLogEntry()
    {
        SerialNumber = string.Empty;
        Message = string.Empty;
    }

    public Guid Id { get; private set; }

    public AlertType Type { get; private set; }

    public AlertSeverity Severity { get; private set; }

    public AlertAudience Audience { get; private set; }

    public EntityId MeterId { get; private set; }

    public string SerialNumber { get; private set; }

    public string Message { get; private set; }

    public DateTime OccurredAtUtc { get; private set; }

    public bool EmailSent { get; private set; }

    public static AlertLogEntry Create(
        AlertType type,
        AlertSeverity severity,
        AlertAudience audience,
        EntityId meterId,
        string serialNumber,
        string message,
        DateTime occurredAtUtc,
        bool emailSent) => new()
    {
        Id = Guid.NewGuid(),
        Type = type,
        Severity = severity,
        Audience = audience,
        MeterId = meterId,
        SerialNumber = serialNumber,
        Message = message,
        OccurredAtUtc = occurredAtUtc,
        EmailSent = emailSent,
    };

    /// <summary>Reconstructs an entry read back from storage, preserving its original id.</summary>
    public static AlertLogEntry Rehydrate(
        Guid id,
        AlertType type,
        AlertSeverity severity,
        AlertAudience audience,
        EntityId meterId,
        string serialNumber,
        string message,
        DateTime occurredAtUtc,
        bool emailSent) => new()
    {
        Id = id,
        Type = type,
        Severity = severity,
        Audience = audience,
        MeterId = meterId,
        SerialNumber = serialNumber,
        Message = message,
        OccurredAtUtc = occurredAtUtc,
        EmailSent = emailSent,
    };
}
