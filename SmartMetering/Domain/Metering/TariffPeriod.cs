namespace SmartMetering.Domain.Metering;

/// <summary>Tariff classification of a measurement. High = ВТ (07:00–23:00), Low = НТ (23:00–07:00).</summary>
public enum TariffPeriod
{
    High = 0,
    Low = 1,
}
