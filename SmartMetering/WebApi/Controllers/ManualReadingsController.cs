using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.ManualReadings;

namespace SmartMetering.WebApi.Controllers;

[Route("api/manual-readings")]
[Authorize]
public sealed class ManualReadingsController : ApiControllerBase
{
    private readonly IManualReadingService _readings;

    public ManualReadingsController(IManualReadingService readings) => _readings = readings;

    /// <summary>
    /// Consumer submits a manual reading (multipart/form-data) with a photo of the meter display.
    /// New entries start as PendingReview; a blob-triggered function generates an optimized copy of the photo.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Consumer")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> Submit([FromForm] SubmitManualReadingForm form, CancellationToken ct)
    {
        await using var stream = new MemoryStream();
        await form.Image.CopyToAsync(stream, ct);

        var id = await _readings.SubmitAsync(
            CurrentUserId,
            new SubmitManualReadingRequest(form.MeterId, form.DeclaredTotalEnergyKwh, form.Note),
            stream.ToArray(),
            form.Image.ContentType,
            form.Image.FileName,
            ct);

        return Ok(new { id });
    }

    /// <summary>Consumer's own submission history.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Consumer")]
    public async Task<ActionResult<IReadOnlyList<ManualReadingDto>>> GetMine(CancellationToken ct) =>
        Ok(await _readings.GetMyReadingsAsync(CurrentUserId, ct));

    /// <summary>Billing admin: all readings awaiting review, oldest first.</summary>
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<ActionResult<IReadOnlyList<ManualReadingDto>>> GetPending(CancellationToken ct) =>
        Ok(await _readings.GetPendingAsync(ct));

    /// <summary>Billing admin approves — reading moves to Processed and feeds into future billing.</summary>
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<IActionResult> Approve(Guid id, ReviewManualReadingRequest request, CancellationToken ct)
    {
        await _readings.ApproveAsync(CurrentUserId, id, request, ct);
        return NoContent();
    }

    /// <summary>Billing admin rejects — reading is excluded from billing.</summary>
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<IActionResult> Reject(Guid id, ReviewManualReadingRequest request, CancellationToken ct)
    {
        await _readings.RejectAsync(CurrentUserId, id, request, ct);
        return NoContent();
    }

    /// <summary>
    /// Serves a reading's photo (original or optimized). The blob container is private, so the
    /// frontend always goes through this authenticated proxy rather than a raw blob URL.
    /// </summary>
    [HttpGet("images/{*blobName}")]
    public async Task<IActionResult> GetImage(string blobName, CancellationToken ct)
    {
        var isStaff = User.IsInRole("Admin") || User.IsInRole("BillingAdmin");
        var image = await _readings.GetImageAsync(CurrentUserId, isStaff, blobName, ct);
        return File(image.Content, image.ContentType, image.FileName);
    }
}

/// <summary>Multipart/form-data binding model for <see cref="ManualReadingsController.Submit"/>.</summary>
public sealed class SubmitManualReadingForm
{
    public Guid MeterId { get; set; }

    public decimal DeclaredTotalEnergyKwh { get; set; }

    public string? Note { get; set; }

    public IFormFile Image { get; set; } = default!;
}
