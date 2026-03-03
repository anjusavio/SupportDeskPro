/// <summary>
/// Request model for activating or deactivating an SLA policy.
/// </summary>
namespace SupportDeskPro.Contracts.SLAPolicies;

public record UpdateSLAPolicyStatusRequest(bool IsActive);