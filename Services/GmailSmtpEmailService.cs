using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using ClientSphere.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ClientSphere.Services
{
    public class GmailSmtpEmailService : IEmailService, Microsoft.AspNetCore.Identity.UI.Services.IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<GmailSmtpEmailService> _logger;
        private readonly string _senderEmail;
        private readonly string _senderName;
        private readonly string _appPassword;

        public GmailSmtpEmailService(IConfiguration configuration, ILogger<GmailSmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
            _senderEmail = _configuration["GmailSmtp:SenderEmail"] ?? "";
            _senderName = _configuration["GmailSmtp:SenderName"] ?? "ClientSphere";
            _appPassword = _configuration["GmailSmtp:AppPassword"] ?? "";
        }

        /// <summary>
        /// Core method to send an email via Gmail SMTP.
        /// </summary>
        private async Task SendEmailCoreAsync(string toEmail, string subject, string htmlBody)
        {
            using var message = new MailMessage();
            message.From = new MailAddress(_senderEmail, _senderName);
            message.To.Add(new MailAddress(toEmail));
            message.Subject = subject;
            message.Body = htmlBody;
            message.IsBodyHtml = true;

            using var client = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_senderEmail, _appPassword),
                EnableSsl = true
            };

            await client.SendMailAsync(message);
            _logger.LogInformation("Email sent to {Email} with subject: {Subject}", toEmail, subject);
        }

        // ─── IEmailSender (used by ASP.NET Identity for 2FA, password reset, etc.) ───

        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            await SendEmailCoreAsync(email, subject, htmlMessage);
        }

        // ─── IEmailService (used by ClientSphere business logic) ───

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            var subject = "Welcome to ClientSphere!";
            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2 style='color: #1a1a2e;'>Welcome to ClientSphere!</h2>
                    <p><strong>Hello {name}</strong>,</p>
                    <p>We're excited to have you on board. Your account is ready to use.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #999; font-size: 12px;'>ClientSphere CRM</p>
                </div>";
            await SendEmailCoreAsync(email, subject, htmlContent);
        }

        public async Task SendInvoiceEmailAsync(string email, Invoice invoice)
        {
            var subject = $"Invoice {invoice.InvoiceNumber} - ${invoice.Amount}";
            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2 style='color: #1a1a2e;'>Invoice {invoice.InvoiceNumber}</h2>
                    <p><strong>Amount:</strong> ${invoice.Amount}</p>
                    <p><strong>Due Date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>
                    <p><strong>Status:</strong> {invoice.Status}</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #999; font-size: 12px;'>ClientSphere CRM</p>
                </div>";
            await SendEmailCoreAsync(email, subject, htmlContent);
        }

        public async Task SendCampaignEmailAsync(List<string> recipients, Campaign campaign)
        {
            var subject = campaign.Name;
            var htmlContent = $@"
                <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2 style='color: #1a1a2e;'>{campaign.Name}</h2>
                    <p>{campaign.Description}</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #999; font-size: 12px;'>ClientSphere CRM</p>
                </div>";

            foreach (var recipientEmail in recipients)
            {
                try
                {
                    await SendEmailCoreAsync(recipientEmail, subject, htmlContent);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send campaign email to {Email}", recipientEmail);
                }
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var subject = "Password Reset Request";
                var htmlContent = $@"
                    <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                        <h2 style='color: #1a1a2e;'>Password Reset</h2>
                        <p>Click the link below to reset your password:</p>
                        <p><a href='{resetLink}' style='color: #0d6efd; text-decoration: none; font-weight: bold;'>Reset Password</a></p>
                        <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                        <p style='color: #999; font-size: 12px;'>ClientSphere CRM — Security Team</p>
                    </div>";
                await SendEmailCoreAsync(email, subject, htmlContent);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send password reset email to {Email}", email);
                return false;
            }
        }
    }
}
