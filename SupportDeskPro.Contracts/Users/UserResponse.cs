namespace SupportDeskPro.Contracts.Users;

// Response models for user list, detail and agent workload views
public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Role,
    bool IsActive,
    bool IsEmailVerified,
    DateTime? LastLoginAt,
    DateTime CreatedAt
);

public record AgentWorkloadResponse(
    Guid AgentId,
    string FirstName,
    string LastName,
    string Email,
    int OpenTickets,
    int InProgressTickets,
    int ResolvedToday
);

public record AgentSummaryResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email
);