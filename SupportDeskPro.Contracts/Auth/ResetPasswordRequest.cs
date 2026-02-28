namespace SupportDeskPro.Contracts.Auth
{
    public record ResetPasswordRequest(
     string Token,
     string NewPassword,
     string ConfirmPassword
 );
}
