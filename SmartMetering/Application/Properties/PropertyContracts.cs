using System.ComponentModel.DataAnnotations;

namespace SmartMetering.Application.Properties;

public sealed record CreatePropertyRequest(
    [Required, MaxLength(150)] string Name,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(250)] string Address,
    [MaxLength(1000)] string? Description);

public sealed record UpdatePropertyRequest(
    [Required, MaxLength(150)] string Name,
    [Required, MaxLength(100)] string City,
    [Required, MaxLength(250)] string Address,
    [MaxLength(1000)] string? Description);

public sealed record PropertyDto(
    Guid Id,
    string Name,
    string City,
    string Address,
    string? Description,
    DateTime CreatedAtUtc);
