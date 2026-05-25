using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Meters;

namespace SmartMetering.WebApi.Controllers;

[Route("api/meters")]
[Authorize(Roles = "Consumer")]
public sealed class MetersController : ApiControllerBase
{
    private readonly IMeterService _meters;

    public MetersController(IMeterService meters) => _meters = meters;

    /// <summary>Lists meters registered under one of the caller's properties.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<MeterDto>>> GetByProperty([FromQuery] Guid propertyId, CancellationToken ct) =>
        Ok(await _meters.GetByPropertyAsync(CurrentUserId, propertyId, ct));

    /// <summary>Step 1 of pairing: register a meter by its serial number (status becomes Unpaired).</summary>
    [HttpPost]
    public async Task<IActionResult> Register(RegisterMeterRequest request, CancellationToken ct)
    {
        var id = await _meters.RegisterAsync(CurrentUserId, request, ct);
        return Ok(new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateMeterRequest request, CancellationToken ct)
    {
        await _meters.UpdateAsync(CurrentUserId, id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _meters.DeleteAsync(CurrentUserId, id, ct);
        return NoContent();
    }
}
