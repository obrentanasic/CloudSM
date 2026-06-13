using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.Abstractions;

public interface ITariffModelRepository
{
    Task<TariffModel?> GetByIdAsync(EntityId id, CancellationToken ct = default);

    Task<TariffModel?> GetActiveAsync(CancellationToken ct = default);

    Task<IReadOnlyList<TariffModel>> GetAllAsync(CancellationToken ct = default);

    Task AddAsync(TariffModel tariff, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);
}
