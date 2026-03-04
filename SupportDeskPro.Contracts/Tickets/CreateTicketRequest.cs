/// <summary>
/// Request model for Customer to create a new support ticket.
/// </summary>
namespace SupportDeskPro.Contracts.Tickets;

public record CreateTicketRequest(
    string Title,
    string Description,
    Guid CategoryId,
    int Priority
);