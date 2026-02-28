namespace SupportDeskPro.Application.Interfaces
{
    public interface ICurrentTenantService
    {
        Guid? TenantId { get; }
        string? TenantName { get; }
        bool IsSuperAdmin { get; }
        Guid? CurrentUserId { get; }
    }
}
