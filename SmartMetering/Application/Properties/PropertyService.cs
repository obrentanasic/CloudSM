using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.Properties;

namespace SmartMetering.Application.Properties;

public sealed class PropertyService : IPropertyService
{
    private readonly IPropertyRepository _properties;
    private readonly ISmartMeterRepository _meters;

    public PropertyService(IPropertyRepository properties, ISmartMeterRepository meters)
    {
        _properties = properties;
        _meters = meters;
    }

    public async Task<Guid> CreateAsync(EntityId ownerId, CreatePropertyRequest request, CancellationToken ct = default)
    {
        var property = Property.Create(request.Name, request.City, request.Address, request.Description, ownerId);
        await _properties.AddAsync(property, ct);
        await _properties.SaveChangesAsync(ct);
        return property.Id.Value;
    }

    public async Task<IReadOnlyList<PropertyDto>> GetMineAsync(EntityId ownerId, CancellationToken ct = default)
    {
        var items = await _properties.GetByOwnerAsync(ownerId, ct);
        return items.Select(Map).ToList();
    }

    public async Task<PropertyDto> GetByIdAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default)
    {
        var property = await GetOwnedAsync(ownerId, propertyId, ct);
        return Map(property);
    }

    public async Task UpdateAsync(EntityId ownerId, Guid propertyId, UpdatePropertyRequest request, CancellationToken ct = default)
    {
        var property = await GetOwnedAsync(ownerId, propertyId, ct);
        property.Update(request.Name, request.City, request.Address, request.Description);
        await _properties.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(EntityId ownerId, Guid propertyId, CancellationToken ct = default)
    {
        var property = await GetOwnedAsync(ownerId, propertyId, ct);

        if (await _meters.AnyForPropertyAsync(property.Id, ct))
        {
            throw new ConflictException("Не можете обрисати објекат који има регистрована бројила.");
        }

        _properties.Remove(property);
        await _properties.SaveChangesAsync(ct);
    }

    private async Task<Property> GetOwnedAsync(EntityId ownerId, Guid propertyId, CancellationToken ct)
    {
        var property = await _properties.GetByIdAsync(EntityId.From(propertyId), ct);

        // Return 404 (not 403) so we don't reveal that another user's object exists.
        if (property is null || !property.IsOwnedBy(ownerId))
        {
            throw new NotFoundException("Објекат није пронађен.");
        }

        return property;
    }

    private static PropertyDto Map(Property p) =>
        new(p.Id.Value, p.Name, p.City, p.Address, p.Description, p.CreatedAtUtc);
}
