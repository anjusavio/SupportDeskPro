using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Uploads and deletes files in Azure Blob Storage.
/// Each file is stored with a GUID-based name to prevent
/// collisions and avoid exposing original file names in storage.
/// Original file name is preserved in the TicketAttachments table.
/// </summary>
public class BlobStorageService : IBlobStorageService
{
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<BlobStorageService> _logger;

    public BlobStorageService(
        IConfiguration configuration,
        ILogger<BlobStorageService> logger)
    {
        var connectionString = configuration["AzureStorage:ConnectionString"]
            ?? throw new InvalidOperationException(
                "Azure Storage connection string not configured.");

        var containerName = configuration["AzureStorage:ContainerName"]
            ?? "attachments";

        _containerClient = new BlobContainerClient(
            connectionString, containerName);

        _logger = logger;
    }

    public async Task<BlobUploadResult> UploadAsync(
        Stream fileStream,
        string originalFileName,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        // Generate unique stored file name — preserves file extension
        var extension = Path.GetExtension(originalFileName);
        var storedFileName = $"{Guid.NewGuid()}{extension}";

        _logger.LogInformation(
            "Uploading file {OriginalFileName} as {StoredFileName}",
            originalFileName, storedFileName);

        var blobClient = _containerClient.GetBlobClient(storedFileName);

        // Set content type so browser handles download correctly
        var uploadOptions = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders
            {
                ContentType = contentType
            }
        };

        await blobClient.UploadAsync(
            fileStream, uploadOptions, cancellationToken);

        var blobUrl = blobClient.Uri.ToString();
        var fileSizeBytes = fileStream.Length;

        _logger.LogInformation(
            "File uploaded successfully. URL: {BlobUrl}", blobUrl);

        return new BlobUploadResult(storedFileName, blobUrl, fileSizeBytes);
    }

    public async Task DeleteAsync(
        string storedFileName,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var blobClient = _containerClient.GetBlobClient(storedFileName);
            await blobClient.DeleteIfExistsAsync(
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "File deleted from Blob Storage: {StoredFileName}",
                storedFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to delete file from Blob Storage: {StoredFileName}",
                storedFileName);
        }
    }

    /// <summary>
    /// Generates a time-limited SAS URL for secure file download.
    /// URL expires after the specified minutes —24 hour
    /// Container is private — direct URL access is blocked.
    /// SAS URL allows temporary read-only access without making blob public.
    /// </summary>
    public string GenerateSasUrl(string storedFileName, int expiryMinutes = 1440)
    {
        var blobClient = _containerClient.GetBlobClient(storedFileName);

        var sasBuilder = new BlobSasBuilder
        {
            BlobContainerName = _containerClient.Name,
            BlobName = storedFileName,
            Resource = "b", // b = blob
            ExpiresOn = DateTimeOffset.UtcNow
                                .AddMinutes(expiryMinutes) 
        };

        sasBuilder.SetPermissions(BlobSasPermissions.Read); 

        var sasUrl = blobClient.GenerateSasUri(sasBuilder).ToString();

        return sasUrl;
    }
}