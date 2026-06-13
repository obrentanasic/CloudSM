using System.ComponentModel.DataAnnotations;

namespace SmartMetering.Application.Billing;

public sealed record TariffModelDto(
    Guid Id,
    string Name,
    decimal GreenLimitKwh,
    decimal BlueLimitKwh,
    decimal GreenHighPriceRsd,
    decimal GreenLowPriceRsd,
    decimal BlueHighPriceRsd,
    decimal BlueLowPriceRsd,
    decimal RedHighPriceRsd,
    decimal RedLowPriceRsd,
    decimal PowerPriceRsdPerKw,
    decimal SupplierFeeRsd,
    bool IsActive,
    DateTime CreatedAtUtc,
    DateTime? ActivatedAtUtc);

public sealed record CreateTariffModelRequest(
    [Required, MaxLength(100)] string Name,
    [Range(0.001, 100000)] decimal GreenLimitKwh,
    [Range(0.001, 100000)] decimal BlueLimitKwh,
    [Range(0, 100000)] decimal GreenHighPriceRsd,
    [Range(0, 100000)] decimal GreenLowPriceRsd,
    [Range(0, 100000)] decimal BlueHighPriceRsd,
    [Range(0, 100000)] decimal BlueLowPriceRsd,
    [Range(0, 100000)] decimal RedHighPriceRsd,
    [Range(0, 100000)] decimal RedLowPriceRsd,
    [Range(0, 100000)] decimal PowerPriceRsdPerKw,
    [Range(0, 100000)] decimal SupplierFeeRsd);

public sealed record GenerateInvoicesRequest(
    [Range(2000, 2100)] int Year,
    [Range(1, 12)] int Month);

public sealed record GeneratedInvoicesDto(int Year, int Month, int Created, int Skipped);

public sealed record InvoiceDto(
    Guid Id,
    Guid PropertyId,
    Guid MeterId,
    string SerialNumber,
    int Year,
    int Month,
    DateTime IssuedAtUtc,
    decimal HighTariffKwh,
    decimal LowTariffKwh,
    decimal GreenKwh,
    decimal BlueKwh,
    decimal RedKwh,
    decimal TotalKwh,
    decimal TotalAmountRsd,
    int Status);

public sealed record InvoicePageDto(
    IReadOnlyList<InvoiceDto> Items,
    int Page,
    int PageSize,
    int TotalCount);

public sealed record InvoiceFileDto(string FileName, string ContentType, byte[] Content);
