using System.ComponentModel.DataAnnotations;
using SmartMetering.Domain.Meters;

namespace SmartMetering.Application.Meters;

public sealed record RegisterMeterRequest(
    [Required] Guid PropertyId,
    [Required, MaxLength(20)] string SerialNumber,
    [Required] ConnectionType ConnectionType,
    [MaxLength(500)] string? Note);

public sealed record UpdateMeterRequest(
    [Required] ConnectionType ConnectionType,
    [MaxLength(500)] string? Note);

public sealed record MeterDto(
    Guid Id,
    Guid PropertyId,
    string SerialNumber,
    ConnectionType ConnectionType,
    decimal MaxApprovedPowerKw,
    string? Note,
    PairingStatus PairingStatus);
