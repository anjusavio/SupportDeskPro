using MediatR;

namespace SupportDeskPro.Application.Features.Auth.ChangePassword;

public record ChangePasswordCommand(
    Guid UserId,
    string CurrentPassword,
    string NewPassword,
    string ConfirmPassword) : IRequest<ChangePasswordResult>;

public record ChangePasswordResult(string Message);