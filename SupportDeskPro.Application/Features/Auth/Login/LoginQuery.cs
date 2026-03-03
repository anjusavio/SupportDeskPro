using MediatR;
using SupportDeskPro.Contracts.Auth;

namespace SupportDeskPro.Application.Features.Auth.Login;

public record LoginQuery(string Email,string Password) : IRequest<LoginResult>;

public record LoginResult(LoginResponse? Response);