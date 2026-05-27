using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Limits;

namespace SmartMetering.WebApi.Controllers;

[Route("api/limit")]
[Authorize(Roles = "Consumer")]
public sealed class LimitsController : ApiControllerBase
{
    private readonly ILimitService _limits;

    public LimitsController(ILimitService limits) => _limits = limits;

    /// <summary>The caller's current consumption limit, or 204 if none is set.</summary>
    [HttpGet]
    public async Task<ActionResult<LimitDto>> GetMine(CancellationToken ct)
    {
        var limit = await _limits.GetMineAsync(CurrentUserId, ct);
        return limit is null ? NoContent() : Ok(limit);
    }

    [HttpPut]
    public async Task<IActionResult> SetMine(SetLimitRequest request, CancellationToken ct)
    {
        await _limits.SetMineAsync(CurrentUserId, request, ct);
        return NoContent();
    }
}
