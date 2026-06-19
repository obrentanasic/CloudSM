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
}