namespace SmartMetering.Application.Network;

/// <summary>One row of the admin "status mreže" table — one registered meter.</summary>
public sealed record MeterNetworkStatusDto(
    Guid MeterId,
    string SerialNumber,
    int ConnectionType,
    int PairingStatus,
    bool IsOnline,
    DateTime? LastHeartbeatUtc,
    Guid PropertyId,
    string PropertyName,
    Guid OwnerId,
    string OwnerName,
    double MonthConsumptionKwh,
    int? LastInvoiceStatus,
    DateTime? LastInvoiceIssuedAtUtc);

/// <summary>One completed payment, for the admin "pregled uplata" list.</summary>
public sealed record PaymentRecordDto(
    Guid InvoiceId,
    string SerialNumber,
    Guid ConsumerId,
    string ConsumerName,
    int Year,
    int Month,
    decimal TotalAmountRsd,
    DateTime IssuedAtUtc,
    DateTime? PaidAtUtc);

/// <summary>Aggregate counters for the admin "statistika računa" panel.</summary>
public sealed record InvoiceStatisticsDto(
    int TotalInvoices,
    int PaidInvoices,
    int UnpaidInvoices,
    int EmailsSent,
    int EmailsNotSent,
    decimal TotalAmountPaidRsd,
    decimal TotalAmountUnpaidRsd);

/// <summary>One persisted alert, for the admin "pregled upozorenja" list.</summary>
public sealed record AlertLogDto(
    Guid Id,
    int Type,
    int Severity,
    int Audience,
    Guid MeterId,
    string SerialNumber,
    string Message,
    DateTime OccurredAtUtc,
    bool EmailSent);
