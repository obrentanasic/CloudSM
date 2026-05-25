namespace SmartMetering.Application.Abstractions;

public interface IEmailService
{
    Task SendAsync(string toEmail, string subject, string htmlContent, CancellationToken ct = default);
}
