namespace SmartMetering.Application.Abstractions;

public sealed record InvoiceDocument(string FileName, string ContentType, byte[] Content);

public interface IInvoiceDocumentStorage
{
    Task SaveTextAsync(string blobName, string content, CancellationToken ct = default);

    Task SavePdfAsync(string blobName, string content, CancellationToken ct = default);

    Task<InvoiceDocument?> DownloadAsync(string blobName, CancellationToken ct = default);
}
