namespace SupportDeskPro.Contracts.Tickets;

public record AISimilarTicketResponse(
    Guid Id,
    int TicketNumber,
    string Title,
    string CategoryName,
    string Status,
    string? Resolution,
    double SimilarityScore,
    DateTime ResolvedAt
);