using MediatR;
using Microsoft.EntityFrameworkCore;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Contracts.Tickets;
using SupportDeskPro.Domain.Entities;
using SupportDeskPro.Domain.Exceptions;

namespace SupportDeskPro.Application.Features.Tickets.UploadAttachment;

/// <summary>
/// Handles file attachment upload for tickets and comments.
/// 
/// Uploads the file to Azure Blob Storage first.
/// If upload succeeds, saves metadata to TicketAttachments table.
/// If DB save fails, deletes the blob to avoid orphaned files.
/// 
/// Validates:
/// - Ticket exists and is not deleted
/// - File size does not exceed 10MB
/// - File type is in the allowed list
/// </summary>
public class UploadAttachmentCommandHandler
    : IRequestHandler<UploadAttachmentCommand, AttachmentResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IBlobStorageService _blobService;

    // Allowed file types — images, PDFs, Office documents, plain text
    private static readonly string[] AllowedContentTypes =
    {
        "image/jpeg", "image/png", "image/gif", "image/webp",
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "application/vnd.ms-excel",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "text/plain"
    };

    private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

    public UploadAttachmentCommandHandler(
        IApplicationDbContext db,
        IBlobStorageService blobService)
    {
        _db = db;
        _blobService = blobService;
    }

    public async Task<AttachmentResponse> Handle(
        UploadAttachmentCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Validate ticket exists
        var ticket = await _db.Tickets
            .FirstOrDefaultAsync(
                t => t.Id == request.TicketId && !t.IsDeleted,
                cancellationToken)
            ?? throw new NotFoundException("Ticket", request.TicketId);

        // 2. Validate file size
        if (request.FileSizeBytes > MaxFileSizeBytes)
            throw new BusinessValidationException(
                "File size cannot exceed 10MB.");

        // 3. Validate file type
        if (!AllowedContentTypes.Contains(request.ContentType.ToLower()))
            throw new BusinessValidationException(
                "File type not allowed. Supported types: images, PDF, Word, Excel, text.");

        // 4. Upload to Azure Blob Storage
        var uploadResult = await _blobService.UploadAsync(
            request.FileStream,
            request.OriginalFileName,
            request.ContentType,
            cancellationToken);

        // 5. Save metadata to database
        var attachment = new TicketAttachment
        {
            TenantId = request.TenantId,
            TicketId = request.TicketId,
            CommentId = request.CommentId,
            UploadedById = request.UploadedById,
            OriginalFileName = request.OriginalFileName,
            StoredFileName = uploadResult.StoredFileName,
            BlobUrl = uploadResult.BlobUrl,
            FileSizeBytes = uploadResult.FileSizeBytes,
            ContentType = request.ContentType,
        };

        _db.TicketAttachments.Add(attachment);

        try
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // DB save failed — delete blob to avoid orphaned files 
            await _blobService.DeleteAsync(
                uploadResult.StoredFileName, cancellationToken);
            throw;
        }

        return new AttachmentResponse(
            attachment.Id,
            attachment.OriginalFileName,
            attachment.FileSizeBytes,
            attachment.ContentType,
            attachment.BlobUrl,
            attachment.CreatedAt);
    }
}