using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Properties;

/// <summary>A consumer's physical object (house, flat, cottage) that smart meters are attached to.</summary>
public class Property : AggregateRoot
{
    private Property()
    {
        Name = string.Empty;
        City = string.Empty;
        Address = string.Empty;
    }

    private Property(string name, string city, string address, string? description, EntityId ownerId)
    {
        Name = name;
        City = city;
        Address = address;
        Description = description;
        OwnerId = ownerId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; }

    public string City { get; private set; }

    public string Address { get; private set; }

    public string? Description { get; private set; }

    public EntityId OwnerId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public static Property Create(string name, string city, string address, string? description, EntityId ownerId) =>
        new(name.Trim(), city.Trim(), address.Trim(), description?.Trim(), ownerId);

    public void Update(string name, string city, string address, string? description)
    {
        Name = name.Trim();
        City = city.Trim();
        Address = address.Trim();
        Description = description?.Trim();
    }

    public bool IsOwnedBy(EntityId userId) => OwnerId.Equals(userId);
}
