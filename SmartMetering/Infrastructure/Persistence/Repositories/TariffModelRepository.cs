using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class TariffModelRepository : ITariffModelRepository
{
    private readonly AppDbContext _db;

    public TariffModelRepository(AppDbContext db) => _db = db;

    public Task<TariffModel?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        _db.TariffModels.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<TariffModel?> GetActiveAsync(CancellationToken ct = default) =>
        _db.TariffModels.FirstOrDefaultAsync(t => t.IsActive, ct);

    public async Task<IReadOnlyList<TariffModel>> GetAllAsync(CancellationToken ct = default) =>
        await _db.TariffModels
            .OrderByDescending(t => t.IsActive)
            .ThenByDescending(t => t.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(TariffModel tariff, CancellationToken ct = default) =>
        await _db.TariffModels.AddAsync(tariff, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
