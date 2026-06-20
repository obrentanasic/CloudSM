using SmartMetering.Domain.Common;

namespace SmartMetering.Domain.ManualReadings;

/// <summary>
/// A consumer-submitted manual meter reading, used when the device is faulty or offline.
/// Carries a photo of the meter display as proof. Starts as PendingReview; a billing
/// administrator inspects the photo against the declared value and either approves it
/// (Processed — included in billing) or rejects it (Rejected — excluded from billing).
/// </summary>
public sealed class ManualReading : AggregateRoot
{
    private ManualReading()
    {
        SerialNumber = string.Empty;
        OriginalImageBlobName = string.Empty;
    }

    private ManualReading(
        EntityId meterId,
        string serialNumber,
        EntityId consumerId,
        decimal declaredTotalEnergyKwh,
        string? note,
        string originalImageBlobName)
    {
        MeterId = meterId;
        SerialNumber = serialNumber;
        ConsumerId = consumerId;
        DeclaredTotalEnergyKwh = declaredTotalEnergyKwh;
        Note = note?.Trim();
        OriginalImageBlobName = originalImageBlobName;
        Status = ManualReadingStatus.PendingReview;
        SubmittedAtUtc = DateTime.UtcNow;
    }

    public EntityId MeterId { get; private set; }

    public string SerialNumber { get; private set; }

    public EntityId ConsumerId { get; private set; }

    public decimal DeclaredTotalEnergyKwh { get; private set; }

    public string? Note { get; private set; }

    public string OriginalImageBlobName { get; private set; }

    /// <summary>Set asynchronously by the blob-triggered optimization function. Null until processed.</summary>
    public string? OptimizedImageBlobName { get; private set; }

    public ManualReadingStatus Status { get; private set; }

    public DateTime SubmittedAtUtc { get; private set; }

    public DateTime? ReviewedAtUtc { get; private set; }

    public EntityId? ReviewedByUserId { get; private set; }

    public string? ReviewNote { get; private set; }

    public static ManualReading Submit(
        EntityId meterId,
        string serialNumber,
        EntityId consumerId,
        decimal declaredTotalEnergyKwh,
        string? note,
        string originalImageBlobName) =>
        new(meterId, serialNumber.Trim().ToUpperInvariant(), consumerId, declaredTotalEnergyKwh, note, originalImageBlobName);

    public void AttachOptimizedImage(string optimizedImageBlobName) =>
        OptimizedImageBlobName = optimizedImageBlobName;

    public void Approve(EntityId reviewerId, string? reviewNote = null)
    {
        if (Status != ManualReadingStatus.PendingReview)
        {
            throw new InvalidOperationException("Само очитавања на чекању могу бити одобрена.");
        }

        Status = ManualReadingStatus.Processed;
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNote = reviewNote?.Trim();
    }

    public void Reject(EntityId reviewerId, string? reviewNote = null)
    {
        if (Status != ManualReadingStatus.PendingReview)
        {
            throw new InvalidOperationException("Само очитавања на чекању могу бити одбијена.");
        }

        Status = ManualReadingStatus.Rejected;
        ReviewedByUserId = reviewerId;
        ReviewedAtUtc = DateTime.UtcNow;
        ReviewNote = reviewNote?.Trim();
    }
}
