namespace SmartMetering.Application.Alerts;

/// <summary>Serialized onto the alert queue; processed asynchronously into an email.</summary>
public sealed class AlertMessage
{
    public int Type { get; set; }

    public int Severity { get; set; }

    public int Audience { get; set; }

    public Guid MeterId { get; set; }

    public string SerialNumber { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public DateTime OccurredAtUtc { get; set; }

    /// <summary>Set for consumer-targeted alerts (e.g. consumption limit) — the user to notify.</summary>
    public Guid? ConsumerUserId { get; set; }
}
