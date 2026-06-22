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
    private readonly IPropertyRepository _properties;
    private readonly IImageStorage _images;
    private readonly ITelemetryRepository _telemetry;
    private readonly ILogger<ManualReadingService> _logger;

    public ManualReadingService(
        IManualReadingRepository readings,
        ISmartMeterRepository meters,
        IPropertyRepository properties,
        IImageStorage images,
        ITelemetryRepository telemetry,
        ILogger<ManualReadingService> logger)
    {
        _readings = readings;
        _meters = meters;
        _properties = properties;
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

        var property = await _properties.GetByIdAsync(meter.PropertyId, ct);
        if (property is null || !property.IsOwnedBy(consumerId))
        {
            throw new AppException("Бројило није пронађено.", AppException.StatusCodes.NotFound);
        }

        var extension = Path.GetExtension(imageFileName) is { Length: > 0 } ext ? ext : ".jpg";
        // Use the reading's own id in the blob path so the OptimizeReadingImage function (which parses
        // the id out of the path) updates the right row. The image proxy resolves by blob name directly.
        var readingId = EntityId.New();
        var blobName = $"manual-readings/{meter.SerialNumber}/{readingId.Value}/original{extension}";

        var reading = ManualReading.Submit(
            readingId,
            meter.Id,
            meter.SerialNumber,
            consumerId,
            request.DeclaredTotalEnergyKwh,
            request.Note,
            blobName);

        await _readings.AddAsync(reading, ct);
        await _readings.SaveChangesAsync(ct);

        try
        {
            // Commit the row before uploading the blob so the optimization trigger can always find it.
            await _images.SaveOriginalAsync(blobName, imageContent, imageContentType, ct);
        }
        catch
        {
            _readings.Remove(reading);
            await _readings.SaveChangesAsync(ct);
            throw;
        }

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
        // Timestamp it at submission time (when the consumer actually read the meter), not approval
        // time — otherwise a reading approved in a later month would be billed in the wrong period.
        var telemetry = Telemetry.Create(
            meter.Id,
            meter.SerialNumber,
            meter.ConnectionType,
            totalEnergyKwh: (double)reading.DeclaredTotalEnergyKwh,
            currentLoadKw: 0,
            voltageL1: null, voltageL2: null, voltageL3: null,
            currentL1: null, currentL2: null, currentL3: null,
            powerFactorL1: null, powerFactorL2: null, powerFactorL3: null,
            observationTime: reading.SubmittedAtUtc);

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
        // Resolve the reading by the stored blob name itself rather than parsing an id out of the path,
        // so it works regardless of how the path was built (and for readings created before that fix).
        var reading = await _readings.GetByBlobNameAsync(blobName, ct);

        // Consumers may only view photos attached to their own readings; staff (Admin/BillingAdmin) see all.
        // A non-existent reading and a forbidden one both surface as 404, so we don't leak which is which.
        if (reading is null || (!isStaff && reading.ConsumerId != callerId))
        {
            throw new NotFoundException("Слика није пронађена.");
        }

        return await _images.DownloadAsync(blobName, ct)
            ?? throw new NotFoundException("Слика није пронађена.");
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