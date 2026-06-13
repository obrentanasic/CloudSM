using System.Text;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using SmartMetering.Application.Abstractions;

namespace SmartMetering.Infrastructure.Storage;

public sealed class InvoiceBlobStorage : IInvoiceDocumentStorage
{
    private readonly BlobContainerClient _container;

    public InvoiceBlobStorage(StorageOptions options)
    {
        _container = new BlobContainerClient(options.ConnectionString, StorageOptions.InvoicesContainer);
        _container.CreateIfNotExists();
    }

    public async Task SaveTextAsync(string blobName, string content, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "text/plain; charset=utf-8" },
            },
            ct);
    }

    public async Task SavePdfAsync(string blobName, string content, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        using var stream = new MemoryStream(SimplePdfWriter.Create(content));
        await blob.UploadAsync(
            stream,
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = "application/pdf" },
            },
            ct);
    }

    public async Task<InvoiceDocument?> DownloadAsync(string blobName, CancellationToken ct = default)
    {
        var blob = _container.GetBlobClient(blobName);
        if (!await blob.ExistsAsync(ct))
        {
            return null;
        }

        var download = await blob.DownloadContentAsync(ct);
        var name = blobName.Split('/').Last();
        var contentType = download.Value.Details.ContentType ?? "application/octet-stream";
        return new InvoiceDocument(name, contentType, download.Value.Content.ToArray());
    }
}
