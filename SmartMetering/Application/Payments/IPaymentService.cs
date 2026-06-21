using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Payments;

public interface IPaymentService
{
    /// <summary>
    /// Creates a Stripe Checkout Session for the given invoice.
    /// Validates that the invoice belongs to the requesting consumer and is Unpaid.
    /// </summary>
    Task<CheckoutSessionDto> CreateCheckoutSessionAsync(
        EntityId consumerId,
        CreateCheckoutSessionRequest request,
        CancellationToken ct = default);

    /// <summary>
    /// Marks the invoice referenced by the Stripe session as Paid.
    /// Called from the Stripe webhook after successful payment.
    /// </summary>
    Task MarkInvoicePaidAsync(string stripeSessionId, CancellationToken ct = default);

    /// <summary>
    /// Consumer-facing confirmation used by the success page after returning from Checkout: verifies
    /// with Stripe that the session is paid and that the invoice belongs to the caller, then marks it
    /// Paid. A reliable fallback to the webhook for local development. Returns true if (now) paid.
    /// </summary>
    Task<bool> ConfirmCheckoutAsync(EntityId consumerId, string stripeSessionId, CancellationToken ct = default);
}