using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Limits;

namespace SmartMetering.Application.Limits;

public sealed class LimitService : ILimitService
{
    private readonly IConsumptionLimitRepository _limits;

    public LimitService(IConsumptionLimitRepository limits) => _limits = limits;

    public async Task<LimitDto?> GetMineAsync(EntityId userId, CancellationToken ct = default)
    {
        var limit = await _limits.GetByUserAsync(userId, ct);
        return limit is null ? null : new LimitDto(limit.Value, (int)limit.Unit);
    }

    public async Task SetMineAsync(EntityId userId, SetLimitRequest request, CancellationToken ct = default)
    {
        if (request.Value <= 0)
        {
            throw new AppException("Limit mora biti veci od nule.");
        }

        if (!Enum.IsDefined(request.Unit))
        {
            throw new AppException("Nepoznata jedinica limita.");
        }

        var limit = await _limits.GetByUserAsync(userId, ct);
        if (limit is null)
        {
            limit = ConsumptionLimit.Create(userId, request.Value, request.Unit);
            await _limits.AddAsync(limit, ct);
        }
        else
        {
            limit.Update(request.Value, request.Unit);
        }

        await _limits.SaveChangesAsync(ct);
    }
}
