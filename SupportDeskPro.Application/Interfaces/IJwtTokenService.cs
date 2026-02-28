using SupportDeskPro.Domain.Entities;

namespace SupportDeskPro.Application.Interfaces
{
    public interface IJwtTokenService
    {
        string GenerateAccessToken(User user, string tenantName);
        string GenerateRefreshToken();
        Guid? GetUserIdFromToken(string token);
    }
}
