/// <summary>
/// Response models for ticket list, detail and history views.
/// </summary>
namespace SupportDeskPro.Contracts.Tickets;

public record TicketResponse(
    Guid Id,
    int TicketNumber,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid CategoryId,
    string CategoryName,
    Guid CustomerId,
    string CustomerName,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    DateTime? SLAFirstResponseDueAt,
    DateTime? SLAResolutionDueAt,
    DateTime? FirstResponseAt,
    DateTime? ResolvedAt,
    bool IsSLABreached,
    DateTime LastActivityAt,
    DateTime CreatedAt
);

public record TicketAttachmentResponse(
    Guid Id,
    string OriginalFileName,
    long FileSizeBytes,
    string ContentType,
    string BlobUrl,
    DateTime CreatedAt);

public record TicketDetailResponse(
    Guid Id,
    int TicketNumber,
    string Title,
    string Description,
    string Status,
    string Priority,
    Guid CategoryId,
    string CategoryName,
    Guid CustomerId,
    string CustomerName,
    string CustomerEmail,
    Guid? AssignedAgentId,
    string? AssignedAgentName,
    string? AssignedAgentEmail,
    DateTime? SLAFirstResponseDueAt,
    DateTime? SLAResolutionDueAt,
    DateTime? FirstResponseAt,
    DateTime? ResolvedAt,
    DateTime? ClosedAt,
    bool IsSLABreached,
    DateTime? SLABreachedAt,
    string? AISuggestedCategoryName,
    string? AISuggestedPriority,
    decimal? AICategorizationConfidence,
    DateTime LastActivityAt,
    DateTime CreatedAt,
    List<TicketAttachmentResponse> Attachments
);

public record TicketStatusHistoryResponse(
    Guid Id,
    string? FromStatus,
    string ToStatus,
    string ChangedByName,
    string? Note,
    DateTime CreatedAt
);