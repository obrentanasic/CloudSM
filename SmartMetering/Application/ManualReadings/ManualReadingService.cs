using Microsoft.Extensions.Logging;
using SmartMetering.Application.Abstractions;
using SmartMetering.Application.Common;
using SmartMetering.Domain.Common;
using SmartMetering.Domain.ManualReadings;
using SmartMetering.Domain.Metering;

namespace SmartMetering.Application.ManualReadings;

public sealed class ManualReadingService : IManualReadingService
{
    private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/jpg", "image/png", "image/webp",
    };

    private const long MaxImageBytes = 8 * 1024 * 1024; // 8 MB

    private readonly IManualReadingRepository _readings;
    private readonly ISmartMeterRepository _meters;
    private readonly IImageStorage _images;
    private readonly ITelemetryRepository _telemetry;
    private readonly ILogger<ManualReadingService> _logger;

    public ManualReadingService(
        IManualReadingRepository readings,
        ISmartMeterRepository meters,
        IImageStorage images,
        ITelemetryRepository telemetry,
        ILogger<ManualReadingService> logger)
    {
        _readings = readings;
        _meters = meters;
        _images = images;
        _telemetry = telemetry;
        _logger = logger;
    }

    public async Task<Guid> SubmitAsync(
        EntityId consumerId,
        SubmitManualReadingRequest request,
        byte[] imageContent,
        string imageContentType,
        string imageFileName,
        CancellationToken ct = default)
    {
        if (request.DeclaredTotalEnergyKwh < 0)
        {
            throw new AppException("Очитано стање не може бити негативно.");
        }

        if (imageContent.Length == 0)
        {
            throw new AppException("Слика дисплеја бројила је обавезна.");
        }

        if (imageContent.Length > MaxImageBytes)
        {
            throw new AppException("Слика је превелика (максимално 8 MB).");
        }

        if (!AllowedContentTypes.Contains(imageContentType))
        {
            throw new AppException("Дозвољени формати слике су JPEG, PNG и WebP.");
        }

        var meter = await _meters.GetByIdAsync(EntityId.From(request.MeterId), ct)
            ?? throw new NotFoundException("Бројило није пронађено.");

        var owner = await _meters.GetByPropertyAsync(meter.PropertyId, ct);
        // Ownership is enforced one level up via the property; re-validated here defensively.
        var consumerOwnsMeter = owner.Any(m => m.Id == meter.Id);
        if (!consumerOwnsMeter)
        {
            throw new AppException("Бројило није пронађено.", AppException.StatusCodes.NotFound);
        }

        var extension = Path.GetExtension(imageFileName) is { Length: > 0 } ext ? ext : ".jpg";
        var readingId = Guid.NewGuid();
        var blobName = $"manual-readings/{meter.SerialNumber}/{readingId}/original{extension}";

        await _images.SaveOriginalAsync(blobName, imageContent, imageContentType, ct);

        var reading = ManualReading.Submit(
            meter.Id,
            meter.SerialNumber,
            consumerId,
            request.DeclaredTotalEnergyKwh,
            request.Note,
            blobName);

        await _readings.AddAsync(reading, ct);
        await _readings.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Manual reading {Id} submitted for meter {Serial} by consumer {ConsumerId}.",
            reading.Id.Value, meter.SerialNumber, consumerId.Value);

        return reading.Id.Value;
    }

    public async Task<IReadOnlyList<ManualReadingDto>> GetMyReadingsAsync(EntityId consumerId, CancellationToken ct = default)
    {
        var readings = await _readings.GetByConsumerAsync(consumerId, ct);
        return readings
            .OrderByDescending(r => r.SubmittedAtUtc)
            .Select(Map)
            .ToList();
    }

    public async Task<IReadOnlyList<ManualReadingDto>> GetPendingAsync(CancellationToken ct = default)
    {
        var readings = await _readings.GetPendingAsync(ct);
        return readings
            .OrderBy(r => r.SubmittedAtUtc)
            .Select(Map)
            .ToList();
    }

    public async Task ApproveAsync(EntityId reviewerId, Guid readingId, ReviewManualReadingRequest request, CancellationToken ct = default)
    {
        var reading = await _readings.GetByIdAsync(EntityId.From(readingId), ct)
            ?? throw new NotFoundException("Очитавање није пронађено.");

        var meter = await _meters.GetByIdAsync(reading.MeterId, ct)
            ?? throw new NotFoundException("Бројило није пронађено.");

        reading.Approve(reviewerId, request.ReviewNote);
        await _readings.SaveChangesAsync(ct);

        // Feed the approved reading into the telemetry stream as a single synthetic point,
        // so it flows through the exact same BillingCalculator logic as device-reported readings.
        var telemetry = Telemetry.Create(
            meter.Id,
            meter.SerialNumber,
            meter.ConnectionType,
            totalEnergyKwh: (double)reading.DeclaredTotalEnergyKwh,
            currentLoadKw: 0,
            voltageL1: null, voltageL2: null, voltageL3: null,
            currentL1: null, currentL2: null, currentL3: null,
            powerFactorL1: null, powerFactorL2: null, powerFactorL3: null,
            observationTime: reading.ReviewedAtUtc ?? DateTime.UtcNow);

        await _telemetry.SaveAsync(telemetry, ct);

        _logger.LogInformation(
            "Manual reading {Id} approved by {ReviewerId} and recorded as telemetry for meter {Serial}.",
            reading.Id.Value, reviewerId.Value, meter.SerialNumber);
    }

    public async Task RejectAsync(EntityId reviewerId, Guid readingId, ReviewManualReadingRequest request, CancellationToken ct = default)
    {
        var reading = await _readings.GetByIdAsync(EntityId.From(readingId), ct)
            ?? throw new NotFoundException("Очитавање није пронађено.");

        reading.Reject(reviewerId, request.ReviewNote);
        await _readings.SaveChangesAsync(ct);

        _logger.LogInformation("Manual reading {Id} rejected by {ReviewerId}.", reading.Id.Value, reviewerId.Value);
    }

    public async Task<ImageFile> GetImageAsync(EntityId callerId, bool isStaff, string blobName, CancellationToken ct = default)
    {
        var readingId = ExtractReadingId(blobName);
        var reading = readingId is null ? null : await _readings.GetByIdAsync(EntityId.From(readingId.Value), ct);

        var belongsToReading = reading is not null
            && (reading.OriginalImageBlobName == blobName || reading.OptimizedImageBlobName == blobName);

        // Consumers may only view photos attached to their own readings; staff (Admin/BillingAdmin) see all.
        // A non-existent reading and a forbidden one both surface as 404, so we don't leak which is which.
        if (!belongsToReading || (!isStaff && reading!.ConsumerId != callerId))
        {
            throw new NotFoundException("Слика није пронађена.");
        }

        return await _images.DownloadAsync(blobName, ct)
            ?? throw new NotFoundException("Слика није пронађена.");
    }

    /// <summary>Blob names are "manual-readings/{serial}/{readingId}/{file}" — the reading id is the 3rd segment.</summary>
    private static Guid? ExtractReadingId(string blobName)
    {
        var segments = blobName.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments.Length == 4 && Guid.TryParse(segments[2], out var id) ? id : null;
    }

    private ManualReadingDto Map(ManualReading r) => new(
        r.Id.Value,
        r.MeterId.Value,
        r.SerialNumber,
        r.ConsumerId.Value,
        r.DeclaredTotalEnergyKwh,
        r.Note,
        _images.GetUrl(r.OriginalImageBlobName),
        r.OptimizedImageBlobName is null ? null : _images.GetUrl(r.OptimizedImageBlobName),
        r.Status.ToString(),
        r.SubmittedAtUtc,
        r.ReviewedAtUtc,
        r.ReviewNote);
}