using SmartMetering.Application.Abstractions;
using SmartMetering.Domain.Common;

namespace SmartMetering.Application.ManualReadings;

public interface IManualReadingService
{
    /// <summary>Consumer submits a manual reading with a photo of the meter display.</summary>
    Task<Guid> SubmitAsync(
        EntityId consumerId,
        SubmitManualReadingRequest request,
        byte[] imageContent,
        string imageContentType,
        string imageFileName,
        CancellationToken ct = default);

    /// <summary>Consumer's own submission history.</summary>
    Task<IReadOnlyList<ManualReadingDto>> GetMyReadingsAsync(EntityId consumerId, CancellationToken ct = default);

    /// <summary>Billing admin: all readings awaiting review.</summary>
    Task<IReadOnlyList<ManualReadingDto>> GetPendingAsync(CancellationToken ct = default);

    /// <summary>Billing admin approves — reading moves to Processed and feeds into future billing.</summary>
    Task ApproveAsync(EntityId reviewerId, Guid readingId, ReviewManualReadingRequest request, CancellationToken ct = default);

    /// <summary>Billing admin rejects — reading is excluded from billing.</summary>
    Task RejectAsync(EntityId reviewerId, Guid readingId, ReviewManualReadingRequest request, CancellationToken ct = default);

    /// <summary>
    /// Serves a reading's photo (original or optimized). Consumers may only view their own readings'
    /// photos; staff (Admin/BillingAdmin) may view any. Throws NotFoundException otherwise.
    /// </summary>
    Task<ImageFile> GetImageAsync(EntityId callerId, bool isStaff, string blobName, CancellationToken ct = default);
}