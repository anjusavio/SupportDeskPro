namespace SupportDeskPro.Application.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailVerificationAsync(string email,
            string firstName, string token);
        Task SendPasswordResetAsync(string email,
            string firstName, string token);
        Task SendTicketCreatedAsync(string email,
            string firstName, int ticketNumber, string title);
        Task SendTicketAssignedAsync(string agentEmail,
            string agentName, int ticketNumber, string title);
        Task SendSLABreachAlertAsync(string adminEmail,
            int ticketNumber, string title);
    }
}
