using MediatR;

namespace SupportDeskPro.Application.Features.Auth.ResetPassword;

public record ResetPasswordCommand(
    string Token,
    string NewPassword,
    string ConfirmPassword) : IRequest<ResetPasswordResult>;

public record ResetPasswordResult(string Message);