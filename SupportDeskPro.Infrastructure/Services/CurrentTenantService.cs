using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

public class CurrentTenantService : ICurrentTenantService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentTenantService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User =>
        _httpContextAccessor.HttpContext?.User;

    public Guid? TenantId
    {
        get
        {
            var tenantIdClaim = User?.FindFirst("TenantId")?.Value;
            return tenantIdClaim != null ? Guid.Parse(tenantIdClaim) : null;
        }
    }

    public string? TenantName =>User?.FindFirst("TenantName")?.Value;

    public bool IsSuperAdmin =>User?.FindFirst(ClaimTypes.Role)?.Value == "SuperAdmin";

    public Guid? CurrentUserId
    {
        get
        {
            var claim = User?.FindFirst(
                System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            return !string.IsNullOrEmpty(claim)
                ? Guid.Parse(claim)
                : null;
        }
    }
}