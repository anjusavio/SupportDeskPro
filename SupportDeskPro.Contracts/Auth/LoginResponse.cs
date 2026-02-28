namespace SupportDeskPro.Contracts.Auth
{
    //Record - set once, never changed
    public record LoginResponse(
     string AccessToken,
     string RefreshToken,
     int ExpiresIn,
     UserDto User
    );

    public record UserDto(
        Guid Id,
        string FirstName,
        string LastName,
        string Email,
        string Role,
        Guid? TenantId,
        string? TenantName
    );
}
