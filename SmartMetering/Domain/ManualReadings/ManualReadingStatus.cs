namespace SmartMetering.Domain.ManualReadings;

/// <summary>
/// Lifecycle of a manually submitted meter reading.
/// Per spec: "сваки ручни унос се ставља у иницијално стање на чекању" and, once approved,
/// "очитавање прелази у стање обрађено". Readings still "необрађени" are excluded from billing.
/// </summary>
public enum ManualReadingStatus
{
    PendingReview = 0,
    Processed = 1,
    Rejected = 2,
}