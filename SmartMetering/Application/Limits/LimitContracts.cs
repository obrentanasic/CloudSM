using System.ComponentModel.DataAnnotations;
using SmartMetering.Domain.Limits;

namespace SmartMetering.Application.Limits;

public sealed record SetLimitRequest([Range(0, double.MaxValue)] decimal Value, LimitUnit Unit);

public sealed record LimitDto(decimal Value, int Unit);
