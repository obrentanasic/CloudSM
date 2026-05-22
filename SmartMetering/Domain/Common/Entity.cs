namespace SmartMetering.Domain.Common;

public abstract class Entity
{
    public EntityId Id { get; protected set; }

    protected Entity() => Id = EntityId.New();

    protected Entity(EntityId id) => Id = id;

    public override bool Equals(object? obj) =>
        obj is Entity other && GetType() == other.GetType() && Id.Equals(other.Id);

    public override int GetHashCode() => Id.GetHashCode();
}
