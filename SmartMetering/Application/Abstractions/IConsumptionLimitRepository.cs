using SmartMetering.Domain.Common;
using SmartMetering.Domain.Limits;

namespace SmartMetering.Application.Abstractions;

public interface IConsumptionLimitRepository
{
    Task<ConsumptionLimit?> GetByUserAsync(EntityId userId, CancellationToken ct = default);

    Task AddAsync(ConsumptionLimit limit, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
