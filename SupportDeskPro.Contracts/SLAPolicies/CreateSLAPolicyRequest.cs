/// <summary>
/// Request model for creating a new SLA policy for a specific ticket priority.
/// </summary>
namespace SupportDeskPro.Contracts.SLAPolicies;

public record CreateSLAPolicyRequest(
    string Name,
    int Priority,
    int FirstResponseTimeMinutes,
    int ResolutionTimeMinutes
);