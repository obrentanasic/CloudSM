using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Analytics;

namespace SmartMetering.WebApi.Controllers;

[Route("api")]
[Authorize(Roles = "Consumer")]
public sealed class TelemetryController : ApiControllerBase
{
    private readonly IAnalyticsService _analytics;

    public TelemetryController(IAnalyticsService analytics) => _analytics = analytics;

    /// <summary>Recent measurements for a meter (newest first) — feeds the trend/bar charts.</summary>
    [HttpGet("meters/{meterId:guid}/telemetry")]
    public async Task<ActionResult<IReadOnlyList<TelemetryPointDto>>> GetRecent(
        Guid meterId, [FromQuery] int take = 100, CancellationToken ct = default) =>
        Ok(await _analytics.GetRecentTelemetryAsync(CurrentUserId, meterId, take, ct));

    /// <summary>Current snapshot for a single meter (status card).</summary>
    [HttpGet("meters/{meterId:guid}/status")]
    public async Task<ActionResult<MeterStatusDto>> GetStatus(Guid meterId, CancellationToken ct)
    {
        var status = await _analytics.GetMeterStatusAsync(CurrentUserId, meterId, ct);
        return status is null ? NoContent() : Ok(status);
    }

    /// <summary>Current snapshots for all meters under a property (initial dashboard load before SignalR).</summary>
    [HttpGet("properties/{propertyId:guid}/live")]
    public async Task<ActionResult<IReadOnlyList<MeterStatusDto>>> GetPropertyLive(Guid propertyId, CancellationToken ct) =>
        Ok(await _analytics.GetPropertyLiveAsync(CurrentUserId, propertyId, ct));
}
