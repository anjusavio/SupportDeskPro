using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using SupportDeskPro.Application.Interfaces;
using SupportDeskPro.Infrastructure.Settings;

namespace SupportDeskPro.Infrastructure.Services;

/// <summary>
/// Real SMTP email implementation using MailKit.
/// All emails use HTML templates for professional appearance.
/// Configured via EmailSettings in appsettings.json.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings)
    {
        _settings = settings.Value;
    }

    // ─── Core send method ─────────────────────────────────────────────────────

    /// <summary>
    /// Core method — builds MimeMessage and sends via SMTP.
    /// All other methods call this after building their HTML body.
    /// </summary>
    private async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var message = new MimeMessage();

        // From
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));

        // To
        message.To.Add(new MailboxAddress(toName, toEmail));

        // Subject
        message.Subject = subject;

        // Body — HTML + plain text fallback
        var bodyBuilder = new BodyBuilder
        {
            HtmlBody = htmlBody,
            TextBody = StripHtml(htmlBody), // plain text fallback for email clients that don't render HTML
        };
        message.Body = bodyBuilder.ToMessageBody();

        // Send via SMTP
        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }

    // ─── 1. Email Verification ────────────────────────────────────────────────

    /// <summary>
    /// Sent after user registers — contains email verification link.
    /// User must verify before logging in.
    /// </summary>
    public async Task SendEmailVerificationAsync(string email, string firstName, string token)
    {
        var verifyUrl = $"{_settings.FrontendUrl}/verify-email?token={token}";

        var body = HtmlTemplate(
            title: "Verify Your Email Address",
            preheader: "Please verify your email to activate your account.",
            content: $"""
                <p>Hi <strong>{firstName}</strong>,</p>
                <p>Welcome to <strong>SupportDesk Pro</strong>! Please verify your email address to activate your account.</p>
                <p>Click the button below to verify your email:</p>
                {ActionButton(verifyUrl, "Verify Email Address", "#4F46E5")}
                <p style="color:#6B7280;font-size:13px;">This link expires in 24 hours. If you didn't create an account, you can safely ignore this email.</p>
            """
        );

        await SendAsync(email, firstName, "Verify your SupportDesk Pro account", body);
    }

    // ─── 2. Password Reset ────────────────────────────────────────────────────

    /// <summary>
    /// Sent when user clicks Forgot Password.
    /// Token expires in 1 hour for security.
    /// </summary>
    public async Task SendPasswordResetAsync(string email, string firstName, string token)
    {
        var resetUrl = $"{_settings.FrontendUrl}/reset-password?token={token}";

        var body = HtmlTemplate(
            title: "Reset Your Password",
            preheader: "You requested a password reset for your SupportDesk Pro account.",
            content: $"""
                <p>Hi <strong>{firstName}</strong>,</p>
                <p>We received a request to reset your password for your <strong>SupportDesk Pro</strong> account.</p>
                <p>Click the button below to reset your password:</p>
                {ActionButton(resetUrl, "Reset Password", "#4F46E5")}
                <p style="color:#6B7280;font-size:13px;">This link expires in <strong>1 hour</strong>. If you didn't request a password reset, please ignore this email — your password will remain unchanged.</p>
            """
        );

        await SendAsync(email, firstName, "Reset your SupportDesk Pro password", body);
    }

    // ─── 3. Ticket Created — Confirmation to Customer ─────────────────────────

    /// <summary>
    /// Sent to customer immediately after they raise a ticket.
    /// Confirms ticket number and sets expectations.
    /// </summary>
    public async Task SendTicketCreatedAsync(string email, string firstName, int ticketNumber, string title)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/my-tickets";

        var body = HtmlTemplate(
            title: $"Ticket #{ticketNumber} Created",
            preheader: $"Your support ticket has been received.",
            content: $"""
                <p>Hi <strong>{firstName}</strong>,</p>
                <p>Your support ticket has been received and our team will get back to you shortly.</p>
                {InfoBox($"""
                    <strong>Ticket Number:</strong> #{ticketNumber}<br/>
                    <strong>Subject:</strong> {title}<br/>
                    <strong>Status:</strong> Open
                """)}
                <p>You can track the progress of your ticket by clicking the button below:</p>
                {ActionButton(ticketUrl, "View My Tickets", "#4F46E5")}
                <p style="color:#6B7280;font-size:13px;">Our support team typically responds within the SLA time defined for your ticket priority.</p>
            """
        );

        await SendAsync(email, firstName, $"[#{ticketNumber}] Support ticket received — {title}", body);
    }

    // ─── 4. Ticket Assigned — Notification to Agent ───────────────────────────

    /// <summary>
    /// Sent to agent when admin assigns a ticket to them.
    /// Includes ticket details and direct link.
    /// </summary>
    public async Task SendTicketAssignedAsync(string agentEmail, string agentName, int ticketNumber, string title)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/tickets";

        var body = HtmlTemplate(
            title: $"Ticket #{ticketNumber} Assigned to You",
            preheader: $"A new ticket has been assigned to you.",
            content: $"""
                <p>Hi <strong>{agentName}</strong>,</p>
                <p>A support ticket has been assigned to you. Please review and respond within the SLA deadline.</p>
                {InfoBox($"""
                    <strong>Ticket Number:</strong> #{ticketNumber}<br/>
                    <strong>Subject:</strong> {title}<br/>
                    <strong>Action Required:</strong> Please respond to the customer
                """)}
                {ActionButton(ticketUrl, "View Ticket", "#4F46E5")}
                <p style="color:#EF4444;font-size:13px;">⚠️ Please ensure you respond within the SLA deadline to avoid a breach.</p>
            """
        );

        await SendAsync(agentEmail, agentName, $"[#{ticketNumber}] New ticket assigned — {title}", body);
    }

    // ─── 5. New Reply — Notify Customer when Agent Replies ────────────────────

    /// <summary>
    /// Sent to customer when agent adds a public reply to their ticket.
    /// Internal notes do NOT trigger this email.
    /// </summary>
    public async Task SendNewReplyToCustomerAsync(
        string customerEmail, string customerName,
        int ticketNumber, string ticketTitle, string agentName, string replyPreview)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/my-tickets";

        var body = HtmlTemplate(
            title: $"New Reply on Ticket #{ticketNumber}",
            preheader: $"{agentName} replied to your ticket.",
            content: $"""
                <p>Hi <strong>{customerName}</strong>,</p>
                <p><strong>{agentName}</strong> has replied to your support ticket.</p>
                {InfoBox($"""
                    <strong>Ticket:</strong> #{ticketNumber} — {ticketTitle}<br/>
                    <strong>Reply Preview:</strong><br/>
                    <em style="color:#374151;">{TruncateReply(replyPreview)}</em>
                """)}
                {ActionButton(ticketUrl, "View Full Reply", "#4F46E5")}
            """
        );

        await SendAsync(customerEmail, customerName, $"[#{ticketNumber}] New reply from {agentName}", body);
    }

    // ─── 6. New Reply — Notify Agent when Customer Replies ────────────────────

    /// <summary>
    /// Sent to assigned agent when customer adds a reply to the ticket.
    /// Keeps agent informed without them needing to manually check.
    /// </summary>
    public async Task SendNewReplyToAgentAsync(
        string agentEmail, string agentName,
        int ticketNumber, string ticketTitle, string customerName, string replyPreview)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/tickets";

        var body = HtmlTemplate(
            title: $"Customer Replied on Ticket #{ticketNumber}",
            preheader: $"{customerName} replied to ticket #{ticketNumber}.",
            content: $"""
                <p>Hi <strong>{agentName}</strong>,</p>
                <p>The customer <strong>{customerName}</strong> has replied to a ticket assigned to you.</p>
                {InfoBox($"""
                    <strong>Ticket:</strong> #{ticketNumber} — {ticketTitle}<br/>
                    <strong>Customer Reply:</strong><br/>
                    <em style="color:#374151;">{TruncateReply(replyPreview)}</em>
                """)}
                {ActionButton(ticketUrl, "View & Respond", "#4F46E5")}
            """
        );

        await SendAsync(agentEmail, agentName, $"[#{ticketNumber}] Customer replied — {ticketTitle}", body);
    }

    // ─── 7. Status Changed — Notify Customer ──────────────────────────────────

    /// <summary>
    /// Sent to customer when agent changes ticket status.
    /// Keeps customer informed of progress without logging in.
    /// </summary>
    public async Task SendStatusChangedAsync(
        string customerEmail, string customerName,
        int ticketNumber, string ticketTitle, string oldStatus, string newStatus)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/my-tickets";

        var statusColor = newStatus switch
        {
            "Resolved" => "#10B981",
            "Closed" => "#6B7280",
            "OnHold" => "#F97316",
            _ => "#4F46E5",
        };

        var body = HtmlTemplate(
            title: $"Ticket #{ticketNumber} Status Updated",
            preheader: $"Your ticket status changed to {newStatus}.",
            content: $"""
                <p>Hi <strong>{customerName}</strong>,</p>
                <p>The status of your support ticket has been updated.</p>
                {InfoBox($"""
                    <strong>Ticket:</strong> #{ticketNumber} — {ticketTitle}<br/>
                    <strong>Previous Status:</strong> {oldStatus}<br/>
                    <strong>New Status:</strong> <span style="color:{statusColor};font-weight:bold;">{newStatus}</span>
                """)}
                {(newStatus == "Resolved"
                    ? "<p>🎉 Your ticket has been resolved! If the issue persists, you can reopen it by replying.</p>"
                    : "<p>You can view the full ticket details by clicking below:</p>"
                )}
                {ActionButton(ticketUrl, "View Ticket", statusColor)}
            """
        );

        await SendAsync(customerEmail, customerName, $"[#{ticketNumber}] Status updated to {newStatus}", body);
    }

    // ─── 8. SLA Breach Alert — Notify Admin ───────────────────────────────────

    /// <summary>
    /// Sent to admin when a ticket breaches its SLA deadline.
    /// Background job triggers this every 5 minutes for new breaches.
    /// </summary>
    public async Task SendSLABreachAlertAsync(string adminEmail, int ticketNumber, string title)
    {
        var ticketUrl = $"{_settings.FrontendUrl}/tickets";

        var body = HtmlTemplate(
            title: $"⚠️ SLA Breach — Ticket #{ticketNumber}",
            preheader: $"Ticket #{ticketNumber} has breached its SLA deadline.",
            content: $"""
                <p>Hi Admin,</p>
                <p style="color:#EF4444;font-weight:bold;">A ticket has breached its SLA deadline and requires immediate attention.</p>
                {InfoBox($"""
                    <strong>Ticket Number:</strong> #{ticketNumber}<br/>
                    <strong>Subject:</strong> {title}<br/>
                    <strong>Status:</strong> <span style="color:#EF4444;font-weight:bold;">SLA BREACHED</span>
                """, borderColor: "#EF4444")}
                <p>Please assign or escalate this ticket immediately to maintain customer satisfaction.</p>
                {ActionButton(ticketUrl, "View Breached Ticket", "#EF4444")}
            """
        );

        await SendAsync(adminEmail, "Admin", $"🚨 SLA Breach Alert — Ticket #{ticketNumber}", body);
    }

    // ─── 9. Agent Invite ──────────────────────────────────────────────────────

    /// <summary>
    /// Sent when Admin invites a new Agent.
    /// Contains temporary password — agent should change on first login.
    /// </summary>
    public async Task SendAgentInviteAsync(string email, string firstName, string tempPassword)
    {
        var loginUrl = $"{_settings.FrontendUrl}/login";

        var body = HtmlTemplate(
            title: "You've Been Invited to SupportDesk Pro",
            preheader: "Your agent account has been created.",
            content: $"""
                <p>Hi <strong>{firstName}</strong>,</p>
                <p>You have been invited to join <strong>SupportDesk Pro</strong> as a Support Agent.</p>
                <p>Here are your temporary login credentials:</p>
                {InfoBox($"""
                    <strong>Email:</strong> {email}<br/>
                    <strong>Temporary Password:</strong> <code style="background:#F3F4F6;padding:2px 6px;border-radius:4px;">{tempPassword}</code>
                """)}
                <p style="color:#EF4444;font-size:13px;">⚠️ Please change your password immediately after your first login.</p>
                {ActionButton(loginUrl, "Login to SupportDesk Pro", "#4F46E5")}
            """
        );

        await SendAsync(email, firstName, "You've been invited to SupportDesk Pro", body);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // HTML TEMPLATE HELPERS
    // Centralised HTML so all emails look consistent 
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Master HTML email template — wraps all email content.
    /// Professional design with SupportDesk Pro branding.
    /// </summary>
    private static string HtmlTemplate(string title, string preheader, string content)
    {
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="UTF-8"/>
                <meta name="viewport" content="width=device-width,initial-scale=1.0"/>
                <title>{title}</title>
            </head>
            <body style="margin:0;padding:0;background-color:#F9FAFB;font-family:-apple-system,BlinkMacSystemFont,'Segoe UI',Roboto,sans-serif;">

                <!-- Preheader text (shown in email preview) -->
                <span style="display:none;max-height:0;overflow:hidden;">{preheader}</span>

                <!-- Email wrapper -->
                <table width="100%" cellpadding="0" cellspacing="0" style="background-color:#F9FAFB;padding:40px 20px;">
                    <tr>
                        <td align="center">
                            <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;">

                                <!-- Header -->
                                <tr>
                                    <td style="background-color:#4F46E5;border-radius:12px 12px 0 0;padding:32px 40px;text-align:center;">
                                        <h1 style="color:#FFFFFF;margin:0;font-size:24px;font-weight:700;">
                                            🎫 SupportDesk Pro
                                        </h1>
                                    </td>
                                </tr>

                                <!-- Body -->
                                <tr>
                                    <td style="background-color:#FFFFFF;padding:40px;border-left:1px solid #E5E7EB;border-right:1px solid #E5E7EB;">
                                        <h2 style="color:#111827;font-size:20px;font-weight:600;margin:0 0 20px 0;">{title}</h2>
                                        <div style="color:#374151;font-size:15px;line-height:1.6;">
                                            {content}
                                        </div>
                                    </td>
                                </tr>

                                <!-- Footer -->
                                <tr>
                                    <td style="background-color:#F3F4F6;border-radius:0 0 12px 12px;border:1px solid #E5E7EB;border-top:none;padding:24px 40px;text-align:center;">
                                        <p style="color:#6B7280;font-size:13px;margin:0;">
                                            This email was sent by <strong>SupportDesk Pro</strong>.<br/>
                                            Please do not reply to this email.
                                        </p>
                                    </td>
                                </tr>

                            </table>
                        </td>
                    </tr>
                </table>
            </body>
            </html>
        """;
    }

    /// <summary>
    /// Renders a styled call-to-action button 
    /// </summary>
    private static string ActionButton(string url, string text, string color)
    {
        return $"""
            <div style="text-align:center;margin:28px 0;">
                <a href="{url}" style="background-color:{color};color:#FFFFFF;padding:14px 32px;
                   border-radius:8px;text-decoration:none;font-weight:600;font-size:15px;
                   display:inline-block;">{text}</a>
            </div>
        """;
    }

    /// <summary>
    /// Renders a styled info box for ticket details 
    /// </summary>
    private static string InfoBox(string content, string borderColor = "#4F46E5")
    {
        return $"""
            <div style="background-color:#F9FAFB;border-left:4px solid {borderColor};
                        border-radius:4px;padding:16px 20px;margin:20px 0;
                        font-size:14px;line-height:1.8;color:#374151;">
                {content}
            </div>
        """;
    }

    /// <summary>
    /// Truncates reply preview to 200 chars for email 
    /// </summary>
    private static string TruncateReply(string reply)
    {
        if (string.IsNullOrEmpty(reply)) return string.Empty;
        return reply.Length > 200 ? reply[..200] + "..." : reply;
    }

    /// <summary>
    /// Strips HTML tags for plain text fallback 
    /// </summary>
    private static string StripHtml(string html)
    {
        return System.Text.RegularExpressions.Regex.Replace(html, "<.*?>", string.Empty);
    }
}