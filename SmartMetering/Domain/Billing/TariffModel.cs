using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.Billing;

/// <summary>Prices and zone thresholds used by the monthly billing calculation.</summary>
public sealed class TariffModel : AggregateRoot
{
    private TariffModel()
    {
        Name = string.Empty;
    }

    private TariffModel(
        string name,
        decimal greenLimitKwh,
        decimal blueLimitKwh,
        decimal greenHighPriceRsd,
        decimal greenLowPriceRsd,
        decimal blueHighPriceRsd,
        decimal blueLowPriceRsd,
        decimal redHighPriceRsd,
        decimal redLowPriceRsd,
        decimal powerPriceRsdPerKw,
        decimal supplierFeeRsd)
    {
        Name = name;
        GreenLimitKwh = greenLimitKwh;
        BlueLimitKwh = blueLimitKwh;
        GreenHighPriceRsd = greenHighPriceRsd;
        GreenLowPriceRsd = greenLowPriceRsd;
        BlueHighPriceRsd = blueHighPriceRsd;
        BlueLowPriceRsd = blueLowPriceRsd;
        RedHighPriceRsd = redHighPriceRsd;
        RedLowPriceRsd = redLowPriceRsd;
        PowerPriceRsdPerKw = powerPriceRsdPerKw;
        SupplierFeeRsd = supplierFeeRsd;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public string Name { get; private set; }

    public decimal GreenLimitKwh { get; private set; }

    public decimal BlueLimitKwh { get; private set; }

    public decimal GreenHighPriceRsd { get; private set; }

    public decimal GreenLowPriceRsd { get; private set; }

    public decimal BlueHighPriceRsd { get; private set; }

    public decimal BlueLowPriceRsd { get; private set; }

    public decimal RedHighPriceRsd { get; private set; }

    public decimal RedLowPriceRsd { get; private set; }

    public decimal PowerPriceRsdPerKw { get; private set; }

    public decimal SupplierFeeRsd { get; private set; }

    public bool IsActive { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? ActivatedAtUtc { get; private set; }

    public static TariffModel Create(
        string name,
        decimal greenLimitKwh,
        decimal blueLimitKwh,
        decimal greenHighPriceRsd,
        decimal greenLowPriceRsd,
        decimal blueHighPriceRsd,
        decimal blueLowPriceRsd,
        decimal redHighPriceRsd,
        decimal redLowPriceRsd,
        decimal powerPriceRsdPerKw,
        decimal supplierFeeRsd) =>
        new(
            name.Trim(),
            greenLimitKwh,
            blueLimitKwh,
            greenHighPriceRsd,
            greenLowPriceRsd,
            blueHighPriceRsd,
            blueLowPriceRsd,
            redHighPriceRsd,
            redLowPriceRsd,
            powerPriceRsdPerKw,
            supplierFeeRsd);

    public void Activate()
    {
        IsActive = true;
        ActivatedAtUtc = DateTime.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}
