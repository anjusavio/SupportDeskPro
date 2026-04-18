namespace SupportDeskPro.Application.Interfaces;

/// <summary>
/// Handles file upload and deletion to Azure Blob Storage.
/// Keeps cloud storage logic out of the application layer.
/// </summary>
public interface IBlobStorageService
{
    /// <summary>
    /// Uploads a file to Azure Blob Storage.
    /// Returns the public URL of the uploaded file.
    /// </summary>
    Task<BlobUploadResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a file from Azure Blob Storage by stored file name.
    /// </summary>
    Task DeleteAsync(string storedFileName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generates a time-limited SAS (Shared Access Signature) URL
    /// for secure read-only access to a private blob.
    ///
    /// The container is set to private — direct blob URLs are blocked.
    /// This method generates a signed URL that grants temporary read
    /// access without exposing the storage account credentials or
    /// making the container public.
    /// Read-only permission is enforced — the signed URL cannot be
    /// used to upload, modify, or delete files.
    string GenerateSasUrl(string storedFileName,int expiryMinutes = 1440); // URL valid for 24 hours
}

public record BlobUploadResult(
    string StoredFileName,  // GUID-based name in Blob Storage
    string BlobUrl,         // full URL to access the file
    long FileSizeBytes);