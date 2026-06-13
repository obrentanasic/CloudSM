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

    public Task<int> CountByPropertyAsync(EntityId ownerId, EntityId propertyId, CancellationToken ct = default) =>
        _db.Invoices.CountAsync(i => i.ConsumerId == ownerId && i.PropertyId == propertyId, ct);

    public async Task<IReadOnlyList<Invoice>> GetByPropertyAsync(
        EntityId ownerId,
        EntityId propertyId,
        int skip,
        int take,
        CancellationToken ct = default) =>
        await _db.Invoices
            .Where(i => i.ConsumerId == ownerId && i.PropertyId == propertyId)
            .OrderByDescending(i => i.Year)
            .ThenByDescending(i => i.Month)
            .ThenByDescending(i => i.IssuedAtUtc)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);

    public async Task AddAsync(Invoice invoice, CancellationToken ct = default) =>
        await _db.Invoices.AddAsync(invoice, ct);

    public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
