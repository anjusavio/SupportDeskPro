/// <summary>
/// Request model for updating SLA policy name and response time targets.
/// </summary>
namespace SupportDeskPro.Contracts.SLAPolicies;

public record UpdateSLAPolicyRequest(
    string Name,
    int FirstResponseTimeMinutes,
    int ResolutionTimeMinutes
);