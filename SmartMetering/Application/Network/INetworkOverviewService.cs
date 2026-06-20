namespace SmartMetering.Application.Network;

/// <summary>
/// Admin-only, cross-consumer views for Faza 10: network/meter status, payment review,
/// invoice-sending statistics and alert history.
/// </summary>
public interface INetworkOverviewService
{
    /// <summary>Every registered meter with online/offline status, owner, and current-month consumption.</summary>
    Task<IReadOnlyList<MeterNetworkStatusDto>> GetMeterStatusesAsync(CancellationToken ct = default);

    /// <summary>Most recently completed payments, newest first.</summary>
    Task<IReadOnlyList<PaymentRecordDto>> GetPaymentsAsync(int take, CancellationToken ct = default);

    /// <summary>Counts of generated/paid/unpaid invoices and successfully emailed invoices.</summary>
    Task<InvoiceStatisticsDto> GetInvoiceStatisticsAsync(CancellationToken ct = default);

    /// <summary>Most recent alerts (voltage drop, offline device, load spike, limit breach), newest first.</summary>
    Task<IReadOnlyList<AlertLogDto>> GetRecentAlertsAsync(int take, CancellationToken ct = default);
}
