namespace SmartMetering.Application.Payments;

public sealed record CreateCheckoutSessionRequest(Guid InvoiceId);

public sealed record CheckoutSessionDto(string SessionId, string Url);

public sealed record ConfirmPaymentRequest(string SessionId);

public sealed record PaymentConfirmationDto(bool Paid);
