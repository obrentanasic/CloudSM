namespace SmartMetering.Application.ManualReadings;

public sealed record SubmitManualReadingRequest(
    Guid MeterId,
    decimal DeclaredTotalEnergyKwh,
    string? Note);

public sealed record ManualReadingDto(
    Guid Id,
    Guid MeterId,
    string SerialNumber,
    Guid ConsumerId,
    decimal DeclaredTotalEnergyKwh,
    string? Note,
    string OriginalImageUrl,
    string? OptimizedImageUrl,
    string Status,
    DateTime SubmittedAtUtc,
    DateTime? ReviewedAtUtc,
    string? ReviewNote);

public sealed record ReviewManualReadingRequest(string? ReviewNote);
