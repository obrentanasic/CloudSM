namespace SmartMetering.Domain.Common;

public readonly record struct EntityId(Guid Value)
{
    public static EntityId New() => new(Guid.NewGuid());

    public static EntityId From(Guid value) => new(value);

    public static EntityId Parse(string value) => new(Guid.Parse(value));

    public override string ToString() => Value.ToString();
}
