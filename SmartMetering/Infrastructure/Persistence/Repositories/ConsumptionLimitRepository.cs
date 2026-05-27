using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Limits;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class ConsumptionLimitRepository : IConsumptionLimitRepository
{
    private readonly AppDbContext _db;

    public ConsumptionLimitRepository(AppDbContext db) => _db = db;

    public Task<ConsumptionLimit?> GetByUserAsync(EntityId userId, CancellationToken ct = default) =>
        _db.ConsumptionLimits.FirstOrDefaultAsync(l => l.UserId == userId, ct);

    public async Task AddAsync(ConsumptionLimit limit, CancellationToken ct = default) =>
        await _db.ConsumptionLimits.AddAsync(limit, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
