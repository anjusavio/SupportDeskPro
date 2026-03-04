/// <summary>
/// Request model for Admin to assign ticket to an agent.
/// Null AgentId means unassign the ticket.
/// </summary>
namespace SupportDeskPro.Contracts.Tickets;

public record AssignTicketRequest( Guid? AgentId);