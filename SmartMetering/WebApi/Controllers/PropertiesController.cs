using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Properties;

namespace SmartMetering.WebApi.Controllers;

[Route("api/properties")]
[Authorize(Roles = "Consumer")]
public sealed class PropertiesController : ApiControllerBase
{
    private readonly IPropertyService _properties;

    public PropertiesController(IPropertyService properties) => _properties = properties;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PropertyDto>>> GetMine(CancellationToken ct) =>
        Ok(await _properties.GetMineAsync(CurrentUserId, ct));

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<PropertyDto>> GetById(Guid id, CancellationToken ct) =>
        Ok(await _properties.GetByIdAsync(CurrentUserId, id, ct));

    [HttpPost]
    public async Task<IActionResult> Create(CreatePropertyRequest request, CancellationToken ct)
    {
        var id = await _properties.CreateAsync(CurrentUserId, request, ct);
        return CreatedAtAction(nameof(GetById), new { id }, new { id });
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdatePropertyRequest request, CancellationToken ct)
    {
        await _properties.UpdateAsync(CurrentUserId, id, request, ct);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _properties.DeleteAsync(CurrentUserId, id, ct);
        return NoContent();
    }
}
