using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Payments;

public sealed class PaymentService : IPaymentService
{
    private readonly IInvoiceRepository _invoices;
    private readonly IStripeGateway _stripe;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(
        IInvoiceRepository invoices,
        IStripeGateway stripe,
        ILogger<PaymentService> logger)
    {
        _invoices = invoices;
        _stripe = stripe;
        _logger = logger;
    }

    public async Task<CheckoutSessionDto> CreateCheckoutSessionAsync(
        EntityId consumerId,
        CreateCheckoutSessionRequest request,
        CancellationToken ct = default)
    {
        var invoice = await _invoices.GetByIdAsync(EntityId.From(request.InvoiceId), ct)
            ?? throw new NotFoundException("Račun nije pronađen.");

        if (invoice.ConsumerId != consumerId)
            throw new AppException("Nemate pristup ovom računu.", AppException.StatusCodes.Forbidden);

        if (invoice.Status == InvoiceStatus.Paid)
            throw new ConflictException("Račun je već plaćen.");

        var (sessionId, url) = await _stripe.CreateCheckoutSessionAsync(invoice, ct);

        _logger.LogInformation(
            "Stripe Checkout session {SessionId} created for invoice {InvoiceId}.",
            sessionId, invoice.Id.Value);

        return new CheckoutSessionDto(sessionId, url);
    }

    public async Task MarkInvoicePaidAsync(string stripeSessionId, CancellationToken ct = default)
    {
        var invoiceId = await _stripe.GetInvoiceIdFromSessionAsync(stripeSessionId, ct);
        if (invoiceId is null)
        {
            _logger.LogWarning("Stripe session {SessionId} has no invoice metadata.", stripeSessionId);
            return;
        }

        var invoice = await _invoices.GetByIdAsync(EntityId.From(invoiceId.Value), ct);
        if (invoice is null)
        {
            _logger.LogWarning("Invoice {InvoiceId} from Stripe session {SessionId} not found.", invoiceId, stripeSessionId);
            return;
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            _logger.LogInformation("Invoice {InvoiceId} already marked paid — skipping.", invoiceId);
            return;
        }

        invoice.MarkPaid();
        await _invoices.SaveChangesAsync(ct);

        _logger.LogInformation("Invoice {InvoiceId} marked as Paid via Stripe session {SessionId}.", invoiceId, stripeSessionId);
    }

    public async Task<bool> ConfirmCheckoutAsync(EntityId consumerId, string stripeSessionId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(stripeSessionId))
        {
            return false;
        }

        // Returns null unless Stripe reports the session as paid.
        var invoiceId = await _stripe.GetInvoiceIdFromSessionAsync(stripeSessionId, ct);
        if (invoiceId is null)
        {
            return false;
        }

        var invoice = await _invoices.GetByIdAsync(EntityId.From(invoiceId.Value), ct);
        if (invoice is null)
        {
            return false;
        }

        if (invoice.ConsumerId != consumerId)
        {
            throw new AppException("Nemate pristup ovom računu.", AppException.StatusCodes.Forbidden);
        }

        if (invoice.Status == InvoiceStatus.Paid)
        {
            return true;
        }

        invoice.MarkPaid();
        await _invoices.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Invoice {InvoiceId} confirmed Paid via success-page callback (session {SessionId}).",
            invoiceId, stripeSessionId);
        return true;
    }
}
