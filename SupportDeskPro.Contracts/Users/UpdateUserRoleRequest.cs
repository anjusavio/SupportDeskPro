// Request model for changing a user's role between Agent and Customer
namespace SupportDeskPro.Contracts.Users;

public record UpdateUserRoleRequest(
    int Role
);