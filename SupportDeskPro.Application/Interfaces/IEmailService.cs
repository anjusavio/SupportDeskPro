namespace SupportDeskPro.Application.Interfaces
{
    public interface IEmailService
    {
        // Auth
        Task SendEmailVerificationAsync(string email, string firstName, string token);
        Task SendPasswordResetAsync(string email, string firstName, string token);

        // Tickets
        Task SendTicketCreatedAsync(string email, string firstName, int ticketNumber, string title);
        Task SendTicketAssignedAsync(string agentEmail, string agentName, int ticketNumber, string title);
        Task SendStatusChangedAsync(string customerEmail, string customerName, int ticketNumber, string ticketTitle, string oldStatus, string newStatus);

        // Comments
        Task SendNewReplyToCustomerAsync(string customerEmail, string customerName, int ticketNumber, string ticketTitle, string agentName, string replyPreview);
        Task SendNewReplyToAgentAsync(string agentEmail, string agentName, int ticketNumber, string ticketTitle, string customerName, string replyPreview);

        // SLA
        Task SendSLABreachAlertAsync(string adminEmail, int ticketNumber, string title);

        // Users
        Task SendAgentInviteAsync(string email, string firstName, string tempPassword);
    }
}
