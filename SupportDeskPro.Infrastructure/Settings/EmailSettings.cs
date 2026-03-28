namespace SupportDeskPro.Infrastructure.Settings;

/// <summary>
/// Strongly-typed settings for SMTP email configuration.
/// Bound from appsettings.json EmailSettings section.
/// </summary>
public class EmailSettings
{
    public string SmtpHost { get; set; } = string.Empty;
    public int SmtpPort { get; set; }
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string FrontendUrl { get; set; } = string.Empty;
}