using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Limits;

public enum LimitUnit
{
    Rsd = 0,
    Kwh = 1,
}

/// <summary>A consumer-defined consumption threshold. Alerts fire at most once per month.</summary>
public sealed class ConsumptionLimit : AggregateRoot
{
    private ConsumptionLimit() { }

    private ConsumptionLimit(EntityId userId, decimal value, LimitUnit unit)
    {
        UserId = userId;
        Value = value;
        Unit = unit;
    }

    public EntityId UserId { get; private set; }

    public decimal Value { get; private set; }

    public LimitUnit Unit { get; private set; }

    /// <summary>"yyyy-MM" of the last month an alert was sent (prevents repeat alerts).</summary>
    public string? LastAlertedMonth { get; private set; }

    public static ConsumptionLimit Create(EntityId userId, decimal value, LimitUnit unit) =>
        new(userId, value, unit);

    public void Update(decimal value, LimitUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    public bool ShouldAlert(string currentMonth) => LastAlertedMonth != currentMonth;

    public void MarkAlerted(string currentMonth) => LastAlertedMonth = currentMonth;
}
