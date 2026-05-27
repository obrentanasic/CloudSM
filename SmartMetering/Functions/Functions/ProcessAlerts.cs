using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Alerts;
using SmartMetering.Domain.Alerts;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Users;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Queue-triggered notifier. Turns an alert message into an email: grid alerts go to administrators,
/// consumption-limit alerts go to the consumer who set the limit.
/// </summary>
public sealed class ProcessAlerts
{
    private readonly IEmailService _email;
    private readonly IUserRepository _users;
    private readonly ILogger<ProcessAlerts> _logger;

    public ProcessAlerts(IEmailService email, IUserRepository users, ILogger<ProcessAlerts> logger)
    {
        _email = email;
        _users = users;
        _logger = logger;
    }

    [Function("ProcessAlerts")]
    public async Task Run(
        [QueueTrigger(StorageQueues.Alerts, Connection = "StorageConnectionString")] AlertMessage message,
        CancellationToken ct)
    {
        var severity = (AlertSeverity)message.Severity;
        var subject = $"[{severity}] Smart Metering упозорење — {message.SerialNumber}";
        var html = $"<p>{message.Message}</p><p><small>{message.OccurredAtUtc:yyyy-MM-dd HH:mm} UTC</small></p>";

        var recipients = await ResolveRecipientsAsync(message, ct);
        if (recipients.Count == 0)
        {
            _logger.LogWarning("No recipients for alert {Type} ({Serial}).", (AlertType)message.Type, message.SerialNumber);
            return;
        }

        foreach (var email in recipients)
        {
            await _email.SendAsync(email, subject, html, ct);
        }

        _logger.LogInformation("Alert {Type} emailed to {Count} recipient(s).", (AlertType)message.Type, recipients.Count);
    }

    private async Task<IReadOnlyList<string>> ResolveRecipientsAsync(AlertMessage message, CancellationToken ct)
    {
        if ((AlertAudience)message.Audience == AlertAudience.Consumer && message.ConsumerUserId is { } userId)
        {
            var user = await _users.GetByIdAsync(EntityId.From(userId), ct);
            return user is null ? [] : [user.Email];
        }

        // Admin audience: notify all administrators and billing administrators.
        var admins = await _users.GetByRoleAsync(UserRole.Admin, ct);
        var billing = await _users.GetByRoleAsync(UserRole.BillingAdmin, ct);
        return admins.Concat(billing).Select(u => u.Email).Distinct().ToList();
    }
}
