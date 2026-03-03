/// <summary>
/// Response models for SLA policy list and detail views.
/// </summary>
namespace SupportDeskPro.Contracts.SLAPolicies;

public record SLAPolicyResponse(
    Guid Id,
    string Name,
    string Priority,
    int FirstResponseTimeMinutes,
    int ResolutionTimeMinutes,
    bool IsActive,
    DateTime CreatedAt
);