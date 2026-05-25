using Microsoft.Extensions.Logging;
using SendGrid;
using SendGrid.Helpers.Mail;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;

namespace SmartMetering.Infrastructure.Email;

public sealed class SendGridEmailService : IEmailService
{
    private readonly SendGridOptions _options;
    private readonly ILogger<SendGridEmailService> _logger;

    public SendGridEmailService(SendGridOptions options, ILogger<SendGridEmailService> logger)
    {
        _options = options;
        _logger = logger;
    }

    public async Task SendAsync(string toEmail, string subject, string htmlContent, CancellationToken ct = default)
    {
        var client = new SendGridClient(_options.ApiKey);
        var from = new EmailAddress(_options.FromEmail, _options.FromName);
        var to = new EmailAddress(toEmail);
        var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent);

        var response = await client.SendEmailAsync(message, ct);

        // SendGrid does NOT throw on non-2xx; we must inspect the status ourselves.
        if ((int)response.StatusCode is < 200 or >= 300)
        {
            var body = await response.Body.ReadAsStringAsync(ct);
            _logger.LogError(
                "SendGrid rejected email to {To}. Status {Status}. Body: {Body}",
                toEmail, response.StatusCode, body);
            throw new AppException(
                $"Email sending failed (SendGrid {(int)response.StatusCode}): {body}",
                statusCode: 502);
        }

        _logger.LogInformation("SendGrid accepted email to {To} (status {Status}).", toEmail, response.StatusCode);
    }
}
