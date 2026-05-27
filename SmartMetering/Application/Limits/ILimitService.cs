using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Limits;

public interface ILimitService
{
    Task<LimitDto?> GetMineAsync(EntityId userId, CancellationToken ct = default);

    Task SetMineAsync(EntityId userId, SetLimitRequest request, CancellationToken ct = default);
}
