using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class PropertyRepository : IPropertyRepository
{
    private readonly AppDbContext _db;

    public PropertyRepository(AppDbContext db) => _db = db;

    public Task<Property?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        _db.Properties.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Property>> GetByOwnerAsync(EntityId ownerId, CancellationToken ct = default) =>
        await _db.Properties
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.CreatedAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(Property property, CancellationToken ct = default) =>
        await _db.Properties.AddAsync(property, ct);

    public void Remove(Property property) => _db.Properties.Remove(property);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
