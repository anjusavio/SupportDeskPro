/// <summary>
/// Request model for Admin/Agent to update ticket title, description and category.
/// </summary>
namespace SupportDeskPro.Contracts.Tickets;

public record UpdateTicketRequest(
    string Title,
    string Description,
    Guid CategoryId,
    int Priority
);