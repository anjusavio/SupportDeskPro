// Query model for retrieving single user detail by Id (Admin only)
using MediatR;
using SupportDeskPro.Contracts.Users;

namespace SupportDeskPro.Application.Features.Users.GetUserById;

public record GetUserByIdQuery(Guid UserId) : IRequest<UserResponse>;