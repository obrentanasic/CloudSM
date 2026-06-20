using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartMetering.Application.Billing;
using SmartMetering.Application.Payments;

namespace SmartMetering.WebApi.Controllers;

[Route("api/billing")]
public sealed class BillingController : ApiControllerBase
{
    private readonly IBillingService _billing;
    private readonly IPaymentService _payment;

    public BillingController(IBillingService billing, IPaymentService payment)
    {
        _billing = billing;
        _payment = payment;
    }

    [HttpGet("tariffs")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<ActionResult<IReadOnlyList<TariffModelDto>>> GetTariffs(CancellationToken ct) =>
        Ok(await _billing.GetTariffModelsAsync(ct));

    [HttpGet("tariffs/active")]
    [Authorize(Roles = "Admin,BillingAdmin,Consumer")]
    public async Task<ActionResult<TariffModelDto>> GetActiveTariff(CancellationToken ct)
    {
        var tariff = await _billing.GetActiveTariffAsync(ct);
        return tariff is null ? NoContent() : Ok(tariff);
    }

    [HttpPost("tariffs")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<IActionResult> CreateTariff([FromBody] CreateTariffModelRequest request, [FromQuery] bool activate = true, CancellationToken ct = default)
    {
        var id = await _billing.CreateTariffModelAsync(request, activate, ct);
        return Ok(new { id });
    }

    [HttpPost("tariffs/{id:guid}/activate")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<IActionResult> ActivateTariff(Guid id, CancellationToken ct)
    {
        await _billing.ActivateTariffModelAsync(id, ct);
        return NoContent();
    }

    [HttpPost("generate")]
    [Authorize(Roles = "Admin,BillingAdmin")]
    public async Task<ActionResult<GeneratedInvoicesDto>> Generate(GenerateInvoicesRequest request, CancellationToken ct) =>
        Ok(await _billing.GenerateMonthlyInvoicesAsync(request.Year, request.Month, ct));

    [HttpGet("properties/{propertyId:guid}/invoices")]
    [Authorize(Roles = "Consumer")]
    public async Task<ActionResult<InvoicePageDto>> GetPropertyInvoices(
        Guid propertyId,
        [FromQuery] Guid? meterId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default) =>
        Ok(await _billing.GetPropertyInvoicesAsync(CurrentUserId, propertyId, meterId, from, to, page, pageSize, ct));

    [HttpGet("invoices/{id:guid}/pdf")]
    [Authorize(Roles = "Consumer")]
    public async Task<IActionResult> DownloadPdf(Guid id, CancellationToken ct)
    {
        var file = await _billing.GetInvoicePdfAsync(CurrentUserId, id, ct);
        return File(file.Content, file.ContentType, file.FileName);
    }

    // ─── Stripe ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a Stripe Checkout Session for an unpaid invoice.
    /// Returns the redirect URL that the frontend should navigate to.
    /// </summary>
    [HttpPost("invoices/{id:guid}/checkout-session")]
    [Authorize(Roles = "Consumer")]
    public async Task<ActionResult<CheckoutSessionDto>> CreateCheckoutSession(Guid id, CancellationToken ct)
    {
        var result = await _payment.CreateCheckoutSessionAsync(
            CurrentUserId,
            new CreateCheckoutSessionRequest(id),
            ct);
        return Ok(result);
    }

    // Payment confirmation is handled out-of-band by the StripeWebhook Azure Function,
    // which verifies the Stripe-Signature and marks the invoice Paid.
}