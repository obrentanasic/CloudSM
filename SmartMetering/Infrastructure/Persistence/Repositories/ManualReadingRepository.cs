using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.ManualReadings;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class ManualReadingRepository : IManualReadingRepository
{
    private readonly AppDbContext _db;

    public ManualReadingRepository(AppDbContext db) => _db = db;

    public Task<ManualReading?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        _db.ManualReadings.FirstOrDefaultAsync(r => r.Id == id, ct);

    public async Task<IReadOnlyList<ManualReading>> GetPendingAsync(CancellationToken ct = default) =>
        await _db.ManualReadings
            .Where(r => r.Status == ManualReadingStatus.PendingReview)
            .OrderBy(r => r.SubmittedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ManualReading>> GetByConsumerAsync(EntityId consumerId, CancellationToken ct = default) =>
        await _db.ManualReadings
            .Where(r => r.ConsumerId == consumerId)
            .OrderByDescending(r => r.SubmittedAtUtc)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<ManualReading>> GetProcessedForPeriodAsync(
        EntityId meterId, DateTime fromUtc, DateTime toUtc, CancellationToken ct = default) =>
        await _db.ManualReadings
            .Where(r => r.MeterId == meterId
                        && r.Status == ManualReadingStatus.Processed
                        && r.ReviewedAtUtc != null
                        && r.ReviewedAtUtc >= fromUtc
                        && r.ReviewedAtUtc < toUtc)
            .OrderBy(r => r.ReviewedAtUtc)
            .ToListAsync(ct);

    public async Task AddAsync(ManualReading reading, CancellationToken ct = default) =>
        await _db.ManualReadings.AddAsync(reading, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
