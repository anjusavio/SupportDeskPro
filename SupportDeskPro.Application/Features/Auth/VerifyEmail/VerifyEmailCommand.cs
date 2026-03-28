using MediatR;

namespace SupportDeskPro.Application.Features.Auth.VerifyEmail
{
      public record VerifyEmailCommand(string Token) : IRequest<VerifyEmailResult>;
      public record VerifyEmailResult(string Message);
      public record VerifyEmailRequest(string Token);

}
