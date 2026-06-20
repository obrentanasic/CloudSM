using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using SmartMetering.Application.Payments;
using SmartMetering.Infrastructure.Payments;
using Stripe;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Stripe Webhook (spec: "Stripe Webhook окида Azure функцију"). After a successful Checkout payment
/// Stripe POSTs the event here; we verify the signature, find the matching invoice via session
/// metadata and flip its status to Paid. Anonymous by design — security comes from the signed
/// Stripe-Signature header, not a JWT.
/// </summary>
public sealed class StripeWebhook
{
    private readonly IPaymentService _payment;
    private readonly StripeOptions _options;
    private readonly ILogger<StripeWebhook> _logger;

    public StripeWebhook(IPaymentService payment, StripeOptions options, ILogger<StripeWebhook> logger)
    {
        _payment = payment;
        _options = options;
        _logger = logger;
    }

    [Function("StripeWebhook")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "stripe-webhook")] HttpRequest req,
        CancellationToken ct)
    {
        var json = await new StreamReader(req.Body).ReadToEndAsync(ct);

        try
        {
            var stripeEvent = EventUtility.ConstructEvent(
                json,
                req.Headers["Stripe-Signature"],
                _options.WebhookSecret,
                throwOnApiVersionMismatch: false);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted &&
                stripeEvent.Data.Object is Stripe.Checkout.Session session)
            {
                await _payment.MarkInvoicePaidAsync(session.Id, ct);
            }

            return new OkResult();
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Stripe webhook signature verification failed.");
            return new BadRequestObjectResult(new { error = ex.Message });
        }
    }
}
