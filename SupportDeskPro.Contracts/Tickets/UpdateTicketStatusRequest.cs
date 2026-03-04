/// <summary>
/// Request model for updating ticket status with optional note.
/// </summary>
namespace SupportDeskPro.Contracts.Tickets;

public record UpdateTicketStatusRequest(int Status,string? Note);