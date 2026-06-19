namespace SmartMetering.Application.Payments;

public sealed record CreateCheckoutSessionRequest(Guid InvoiceId);

public sealed record CheckoutSessionDto(string SessionId, string Url);
