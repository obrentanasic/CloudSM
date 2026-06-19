
using BillingInvoice = SmartMetering.Domain.Billing.Invoice;

namespace SmartMetering.Application.Abstractions;

public interface IStripeGateway
{
    /// <summary>Creates a Stripe Checkout session and returns (sessionId, redirectUrl).</summary>
    Task<(string SessionId, string Url)> CreateCheckoutSessionAsync(BillingInvoice invoice, CancellationToken ct = default);

    /// <summary>Retrieves the internal invoice Guid stored in the session's metadata.</summary>
    Task<Guid?> GetInvoiceIdFromSessionAsync(string stripeSessionId, CancellationToken ct = default);
}
