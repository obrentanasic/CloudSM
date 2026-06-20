namespace SmartMetering.Application.Abstractions;

public sealed record ImageFile(string FileName, string ContentType, byte[] Content);

public interface IImageStorage
{
    /// <summary>Uploads the original consumer-submitted photo. Returns the blob name (used as the storage key).</summary>
    Task<string> SaveOriginalAsync(string blobName, byte[] content, string contentType, CancellationToken ct = default);

    Task<ImageFile?> DownloadAsync(string blobName, CancellationToken ct = default);

    /// <summary>API-relative path the frontend uses as an &lt;img&gt; src, proxied through an authenticated endpoint.</summary>
    string GetUrl(string blobName);
}
