// Request model for Admin to invite a new agent by email
namespace SupportDeskPro.Contracts.Users;

public record InviteAgentRequest(
    string FirstName,
    string LastName,
    string Email
);