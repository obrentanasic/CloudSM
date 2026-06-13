using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Common;
using SmartMetering.Application.Billing;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Monthly bill generator. Runs on the first day of each month and bills the previous month.
/// </summary>
public sealed class MonthlyBilling
{
    private readonly IBillingService _billing;
    private readonly ILogger<MonthlyBilling> _logger;

    public MonthlyBilling(IBillingService billing, ILogger<MonthlyBilling> logger)
    {
        _billing = billing;
        _logger = logger;
    }

    [Function("MonthlyBilling")]
    public async Task Run([TimerTrigger("0 0 2 1 * *")] TimerInfo timer, CancellationToken ct)
    {
        var period = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc)
            .AddMonths(-1);

        try
        {
            var result = await _billing.GenerateMonthlyInvoicesAsync(period.Year, period.Month, ct);
            _logger.LogInformation(
                "Monthly billing completed for {Year}-{Month:00}. Created: {Created}, skipped: {Skipped}.",
                result.Year,
                result.Month,
                result.Created,
                result.Skipped);
        }
        catch (AppException ex)
        {
            _logger.LogWarning(ex, "Monthly billing skipped for {Year}-{Month:00}.", period.Year, period.Month);
        }
    }
}
