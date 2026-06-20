using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Network;

namespace SmartMetering.WebApi.Controllers;

/// <summary>
/// Faza 10 — admin-only network/payments/alerts oversight. Everything here is cross-consumer,
/// unlike the Consumer-scoped controllers elsewhere in the API.
/// </summary>
[Route("api/admin/network")]
[Authorize(Roles = "Admin,BillingAdmin")]
public sealed class AdminNetworkController : ApiControllerBase
{
    private readonly INetworkOverviewService _network;

    public AdminNetworkController(INetworkOverviewService network) => _network = network;

    /// <summary>All registered meters with online/offline status, owner and current-month consumption.</summary>
    [HttpGet("meters")]
    public async Task<ActionResult<IReadOnlyList<MeterNetworkStatusDto>>> GetMeters(CancellationToken ct) =>
        Ok(await _network.GetMeterStatusesAsync(ct));

    /// <summary>Recently completed payments across all consumers.</summary>
    [HttpGet("payments")]
    public async Task<ActionResult<IReadOnlyList<PaymentRecordDto>>> GetPayments([FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _network.GetPaymentsAsync(take, ct));

    /// <summary>Counts of generated/paid/unpaid invoices and how many were successfully emailed.</summary>
    [HttpGet("invoice-stats")]
    public async Task<ActionResult<InvoiceStatisticsDto>> GetInvoiceStats(CancellationToken ct) =>
        Ok(await _network.GetInvoiceStatisticsAsync(ct));

    /// <summary>Recent alerts (voltage drop, offline device, load spike, consumption-limit breach).</summary>
    [HttpGet("alerts")]
    public async Task<ActionResult<IReadOnlyList<AlertLogDto>>> GetAlerts([FromQuery] int take = 50, CancellationToken ct = default) =>
        Ok(await _network.GetRecentAlertsAsync(take, ct));
}
