// Request model for any user to update their own profile details
namespace SupportDeskPro.Contracts.Users;

public record UpdateProfileRequest(
    string FirstName,
    string LastName
);