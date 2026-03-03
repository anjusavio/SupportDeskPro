using SupportDeskPro.Application.Interfaces;

namespace SupportDeskPro.Infrastructure.Services;

// Placeholder — real implementation comes later
public class EmailService : IEmailService
{
    public Task SendEmailVerificationAsync(string email,
        string firstName, string token)
    {
        Console.WriteLine( $"[EMAIL] Verification email to {email}, token: {token}");
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email,
        string firstName, string token)
    {
        Console.WriteLine( $"[EMAIL] Password reset email to {email}");
        return Task.CompletedTask;
    }

    public Task SendTicketCreatedAsync(string email,
        string firstName, int ticketNumber, string title)
    {
        Console.WriteLine( $"[EMAIL] Ticket created #{ticketNumber} to {email}");
        return Task.CompletedTask;
    }

    public Task SendTicketAssignedAsync(string agentEmail,
        string agentName, int ticketNumber, string title)
    {
        Console.WriteLine($"[EMAIL] Ticket assigned #{ticketNumber} to {agentEmail}");
        return Task.CompletedTask;
    }

    public Task SendSLABreachAlertAsync(string adminEmail,
        int ticketNumber, string title)
    {
        Console.WriteLine($"[EMAIL] SLA breach alert #{ticketNumber} to {adminEmail}");
        return Task.CompletedTask;
    }

    public Task SendAgentInviteAsync(string email,
    string firstName, string tempPassword)
    {
        Console.WriteLine( $"[EMAIL] Agent invite to {email}, temp password: {tempPassword}");
        return Task.CompletedTask;
    }
}