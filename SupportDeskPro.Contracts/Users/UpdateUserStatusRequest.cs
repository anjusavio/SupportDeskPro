// Request model for activating or deactivating a user account
namespace SupportDeskPro.Contracts.Users;

public record UpdateUserStatusRequest(
    bool IsActive
);