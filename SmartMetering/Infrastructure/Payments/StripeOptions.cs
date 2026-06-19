namespace SmartMetering.Infrastructure.Payments;

public sealed class StripeOptions
{
    public const string SectionName = "Stripe";

    /// <summary>Stripe Secret Key (sk_test_... in Sandbox).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Stripe Webhook Signing Secret (whsec_...).</summary>
    public string WebhookSecret { get; set; } = string.Empty;

    /// <summary>URL the user is redirected to after successful payment.</summary>
    public string SuccessUrl { get; set; } = "http://localhost:5173/payment-success";

    /// <summary>URL the user is redirected to if they cancel the Stripe page.</summary>
    public string CancelUrl { get; set; } = "http://localhost:5173/payment-cancel";
}
