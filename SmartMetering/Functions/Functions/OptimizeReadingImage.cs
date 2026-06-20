using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Processing;
using SmartMetering.Application.Abstractions;
using SmartMetering.Infrastructure.Persistence;
using SmartMetering.Infrastructure.Storage;
using Image = SixLabors.ImageSharp.Image;

namespace SmartMetering.Functions.Functions;

/// <summary>
/// Triggered when a consumer uploads a manual-reading photo to the "meter-readings" container,
/// under "manual-readings/{serial}/{readingId}/original.*". Produces a resized, compressed JPEG
/// at "manual-readings/{serial}/{readingId}/optimized.jpg" and links it back to the ManualReading row.
/// </summary>
public sealed class OptimizeReadingImage
{
    private const int MaxDimensionPx = 1280;
    private const int JpegQuality = 75;

    private readonly IImageStorage _images;
    private readonly AppDbContext _db;
    private readonly ILogger<OptimizeReadingImage> _logger;

    public OptimizeReadingImage(IImageStorage images, AppDbContext db, ILogger<OptimizeReadingImage> logger)
    {
        _images = images;
        _db = db;
        _logger = logger;
    }

    [Function("OptimizeReadingImage")]
    public async Task Run(
        [BlobTrigger("meter-readings/manual-readings/{serial}/{readingId}/{fileName}", Connection = "StorageConnectionString")]
        byte[] content,
        string serial,
        string readingId,
        string fileName,
        CancellationToken ct)
    {
        // Avoid an infinite trigger loop: only process the consumer-uploaded original, never our own output.
        if (!fileName.StartsWith("original", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (!Guid.TryParse(readingId, out var id))
        {
            _logger.LogWarning("OptimizeReadingImage: '{ReadingId}' is not a valid reading id, skipping.", readingId);
            return;
        }

        using var image = Image.Load(content);
        image.Mutate(x => x.Resize(new ResizeOptions
        {
            Mode = ResizeMode.Max,
            Size = new Size(MaxDimensionPx, MaxDimensionPx),
        }));

        using var output = new MemoryStream();
        await image.SaveAsync(output, new JpegEncoder { Quality = JpegQuality }, ct);

        var optimizedBlobName = $"manual-readings/{serial}/{readingId}/optimized.jpg";
        await _images.SaveOriginalAsync(optimizedBlobName, output.ToArray(), "image/jpeg", ct);

        var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE ManualReadings SET OptimizedImageBlobName = {optimizedBlobName} WHERE Id = {id}",
            ct);

        if (rows == 0)
        {
            _logger.LogWarning("OptimizeReadingImage: no ManualReading row found for id {ReadingId}.", id);
            return;
        }

        _logger.LogInformation("Optimized image generated for manual reading {ReadingId}.", id);
    }
}