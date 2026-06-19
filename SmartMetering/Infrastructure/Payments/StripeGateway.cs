using SmartMetering.Application.Abstractions;
using Stripe;
using Stripe.Checkout;
using BillingInvoice = SmartMetering.Domain.Billing.Invoice;

namespace SmartMetering.Infrastructure.Payments;

public sealed class StripeGateway : IStripeGateway
{
    private readonly StripeOptions _options;

    public StripeGateway(StripeOptions options)
    {
        _options = options;
        StripeConfiguration.ApiKey = options.SecretKey;
    }

    public async Task<(string SessionId, string Url)> CreateCheckoutSessionAsync(
        BillingInvoice invoice,
        CancellationToken ct = default)
    {
        // Stripe requires amounts in the smallest currency unit (para = 1/100 RSD).
        // We convert RSD → EUR for Stripe (Stripe doesn't support RSD directly).
        // For sandbox testing we use EUR and treat the value as RSD cents (1 RSD = 1 "cent").
        // In production this should be converted via an exchange rate service.
        var amountInCents = (long)Math.Round(invoice.TotalAmountRsd * 100, 0);

        var lineItems = new List<SessionLineItemOptions>
        {
            new()
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    Currency = "rsd",
                    UnitAmount = amountInCents,
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = $"Račun za el. energiju {invoice.Year}-{invoice.Month:D2}",
                        Description = $"Brojilo: {invoice.SerialNumber} | Ukupno: {invoice.TotalKwh:0.###} kWh",
                    },
                },
                Quantity = 1,
            },
        };

        var createOptions = new SessionCreateOptions
        {
            PaymentMethodTypes = ["card"],
            LineItems = lineItems,
            Mode = "payment",
            SuccessUrl = _options.SuccessUrl + "?session_id={CHECKOUT_SESSION_ID}",
            CancelUrl = _options.CancelUrl,
            Metadata = new Dictionary<string, string>
            {
                ["invoice_id"] = invoice.Id.Value.ToString(),
            },
        };

        var service = new SessionService();
        var session = await service.CreateAsync(createOptions, cancellationToken: ct);
        return (session.Id, session.Url);
    }

    public async Task<Guid?> GetInvoiceIdFromSessionAsync(string stripeSessionId, CancellationToken ct = default)
    {
        var service = new SessionService();
        var session = await service.GetAsync(stripeSessionId, cancellationToken: ct);

        if (session?.Metadata is null)
            return null;

        if (session.Metadata.TryGetValue("invoice_id", out var raw) && Guid.TryParse(raw, out var id))
            return id;

        return null;
    }
}
