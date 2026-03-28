using MediatR;

namespace SupportDeskPro.Application.Features.Auth.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<ForgotPasswordResult>;
public record ForgotPasswordResult(string Message);