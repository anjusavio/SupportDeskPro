namespace SupportDeskPro.Contracts.Tickets;

public record AttachmentResponse(
    Guid Id,
    string OriginalFileName,
    long FileSizeBytes,
    string ContentType,
    string BlobUrl,
    DateTime UploadedAt
);