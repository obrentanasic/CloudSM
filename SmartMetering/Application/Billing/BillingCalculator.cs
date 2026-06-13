using SmartMetering.Domain.Billing;
using SmartMetering.Domain.Meters;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Application.Billing;

public sealed record ConsumptionBreakdown(decimal HighTariffKwh, decimal LowTariffKwh)
{
    public decimal TotalKwh => HighTariffKwh + LowTariffKwh;
}

public sealed record BillingBreakdown(
    decimal GreenHighKwh,
    decimal GreenLowKwh,
    decimal BlueHighKwh,
    decimal BlueLowKwh,
    decimal RedHighKwh,
    decimal RedLowKwh,
    decimal GreenAmountRsd,
    decimal BlueAmountRsd,
    decimal RedAmountRsd,
    decimal FixedAmountRsd,
    decimal TotalAmountRsd);

public static class BillingCalculator
{
    public static ConsumptionBreakdown CalculateConsumption(
        IReadOnlyList<Telemetry> readings,
        Telemetry? previousReading)
    {
        decimal high = 0;
        decimal low = 0;
        var previousTotal = previousReading is null ? (decimal?)null : ToDecimal(previousReading.TotalEnergyKwh);

        foreach (var reading in readings.OrderBy(r => r.ObservationTime))
        {
            var currentTotal = ToDecimal(reading.TotalEnergyKwh);
            if (previousTotal is null)
            {
                previousTotal = currentTotal;
                continue;
            }

            var delta = currentTotal - previousTotal.Value;
            if (delta > 0)
            {
                if (reading.Tariff == TariffPeriod.High)
                {
                    high += delta;
                }
                else
                {
                    low += delta;
                }
            }

            previousTotal = currentTotal;
        }

        return new(RoundKwh(high), RoundKwh(low));
    }

    public static BillingBreakdown CalculateInvoice(ConsumptionBreakdown consumption, SmartMeter meter, TariffModel tariff)
    {
        var total = consumption.TotalKwh;
        var highShare = total == 0 ? 0 : consumption.HighTariffKwh / total;
        var lowShare = total == 0 ? 0 : consumption.LowTariffKwh / total;

        var greenTotal = Math.Min(total, tariff.GreenLimitKwh);
        var blueTotal = Math.Max(0, Math.Min(total - tariff.GreenLimitKwh, tariff.BlueLimitKwh - tariff.GreenLimitKwh));
        var redTotal = Math.Max(0, total - tariff.BlueLimitKwh);

        var greenHigh = RoundKwh(greenTotal * highShare);
        var greenLow = RoundKwh(greenTotal * lowShare);
        var blueHigh = RoundKwh(blueTotal * highShare);
        var blueLow = RoundKwh(blueTotal * lowShare);
        var redHigh = RoundKwh(redTotal * highShare);
        var redLow = RoundKwh(redTotal * lowShare);

        var greenAmount = RoundMoney(greenHigh * tariff.GreenHighPriceRsd + greenLow * tariff.GreenLowPriceRsd);
        var blueAmount = RoundMoney(blueHigh * tariff.BlueHighPriceRsd + blueLow * tariff.BlueLowPriceRsd);
        var redAmount = RoundMoney(redHigh * tariff.RedHighPriceRsd + redLow * tariff.RedLowPriceRsd);
        var fixedAmount = RoundMoney(meter.MaxApprovedPowerKw * tariff.PowerPriceRsdPerKw + tariff.SupplierFeeRsd);
        var totalAmount = RoundMoney(greenAmount + blueAmount + redAmount + fixedAmount);

        return new(
            greenHigh,
            greenLow,
            blueHigh,
            blueLow,
            redHigh,
            redLow,
            greenAmount,
            blueAmount,
            redAmount,
            fixedAmount,
            totalAmount);
    }

    private static decimal ToDecimal(double value) => (decimal)Math.Round(value, 6);

    private static decimal RoundKwh(decimal value) => Math.Round(value, 3, MidpointRounding.AwayFromZero);

    private static decimal RoundMoney(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
