using Microsoft.EntityFrameworkCore;
using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Common;

namespace SmartMetering.Infrastructure.Persistence.Repositories;

public sealed class InvoiceRepository : IInvoiceRepository
{
    private readonly AppDbContext _db;

    public InvoiceRepository(AppDbContext db) => _db = db;

    public Task<Invoice?> GetByIdAsync(EntityId id, CancellationToken ct = default) =>
        _db.Invoices.FirstOrDefaultAsync(i => i.Id == id, ct);

    public Task<bool> ExistsAsync(EntityId meterId, int year, int month, CancellationToken ct = default) =>
        _db.Invoices.AnyAsync(i => i.MeterId == meterId && i.Year == year && i.Month == month, ct);

    public Task<int> CountByPropertyAsync(
        EntityId ownerId,
        EntityId propertyId,
        EntityId? meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        CancellationToken ct = default) =>
        QueryByProperty(ownerId, propertyId, meterId, fromUtc, toUtc).CountAsync(ct);

    public async Task<IReadOnlyList<Invoice>> GetByPropertyAsync(
        EntityId ownerId,
        EntityId propertyId,
        EntityId? meterId,
        DateTime? fromUtc,
        DateTime? toUtc,
        int skip,
        int take,
        CancellationToken ct = default) =>
        await QueryByProperty(ownerId, propertyId, meterId, fromUtc, toUtc)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ThenByDescending(i => i.IssuedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default) =>
        await _db.Invoices.AddAsync(invoice, ct);

    public async Task<IReadOnlyList<Invoice>> GetAllAsync(CancellationToken ct = default) =>
        await _db.Invoices
            .OrderByDescending(i => i.IssuedAtUtc)
            .ToListAsync(ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

    private IQueryable<Invoice> QueryByProperty(
        EntityId ownerId,
        EntityId propertyId,
        EntityId? meterId,
        DateTime? fromUtc,
        DateTime? toUtc)
    {
        var query = _db.Invoices
            .Where(i => i.ConsumerId == ownerId && i.PropertyId == propertyId);

        if (meterId is { } meter)
        {
            query = query.Where(i => i.MeterId == meter);
        }

        if (fromUtc is { } from)
        {
            var fromKey = from.Year * 100 + from.Month;
            query = query.Where(i => i.Year * 100 + i.Month >= fromKey);
        }

        if (toUtc is { } to)
        {
            var toKey = to.Year * 100 + to.Month;
            query = query.Where(i => i.Year * 100 + i.Month <= toKey);
        }

        return query;
    }
}
