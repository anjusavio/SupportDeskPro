// Command model for any authenticated user to update their own profile
using MediatR;

namespace SupportDeskPro.Application.Features.Users.UpdateProfile;

public record UpdateProfileCommand(
    string? UserId,
    string FirstName,
    string LastName
) : IRequest<UpdateProfileResult>;

public record UpdateProfileResult(
    bool Success,
    string Message
);