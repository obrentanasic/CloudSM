using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SmartMetering.Application.Abstractions;

namespace SmartMetering.Infrastructure.Storage;

public sealed class MeterReadingImageStorage : IImageStorage
{
    private readonly BlobContainerClient _container;

    public MeterReadingImageStorage(StorageOptions options)
    {
        _container = new BlobContainerClient(options.ConnectionString, StorageOptions.MeterReadingsContainer);
        _container.CreateIfNotExists(PublicAccessType.None);
    }

    public async Task<string> SaveOriginalAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = contentType } },
            ct);
        return blobName;
    }

    public async Task<ImageFile?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(ct))
        {
            return null;
        }

        var download = await blob.DownloadContentAsync(ct);
        var name = blobName.Split('/').Last();
        var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
        return new ImageFile(name, contentType, download.Value.Content.ToArray());
    }

    /// <summary>
    /// Returns an API-relative path (not a raw blob URI — the container is private).
    /// The frontend fetches this through GET /api/manual-readings/images/{blobName} with its JWT.
    /// </summary>
    public string GetUrl(string blobName) => $"/api/manual-readings/images/{blobName}";
}
