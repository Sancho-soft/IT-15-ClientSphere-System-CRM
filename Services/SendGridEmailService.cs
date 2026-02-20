using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SendGrid;
using SendGrid.Helpers.Mail;
using ClientSphere.Models;
using Microsoft.Extensions.Configuration;

namespace ClientSphere.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _senderEmail;
        private readonly string _senderName;

        public SendGridEmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["SendGrid:ApiKey"];
            _senderEmail = _configuration["SendGrid:SenderEmail"];
            _senderName = _configuration["SendGrid:SenderName"] ?? "ClientSphere";
        }

        public async Task SendWelcomeEmailAsync(string email, string name)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(email, name);
            var subject = "Welcome to ClientSphere!";
            var plainTextContent = $"Hello {name},\n\nWelcome to ClientSphere! We're excited to have you on board.";
            var htmlContent = $"<strong>Hello {name}</strong>,<br><br>Welcome to ClientSphere! We're excited to have you on board.";
            
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }

        public async Task SendInvoiceEmailAsync(string email, Invoice invoice)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var to = new EmailAddress(email);
            var subject = $"Invoice {invoice.InvoiceNumber} - ${invoice.Amount}";
            var plainTextContent = $"Your invoice {invoice.InvoiceNumber} for ${invoice.Amount} is ready. Due date: {invoice.DueDate:yyyy-MM-dd}";
            var htmlContent = $@"
                <h2>Invoice {invoice.InvoiceNumber}</h2>
                <p><strong>Amount:</strong> ${invoice.Amount}</p>
                <p><strong>Due Date:</strong> {invoice.DueDate:yyyy-MM-dd}</p>
                <p><strong>Status:</strong> {invoice.Status}</p>
            ";
            
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            await client.SendEmailAsync(msg);
        }

        public async Task SendCampaignEmailAsync(List<string> recipients, Campaign campaign)
        {
            var client = new SendGridClient(_apiKey);
            var from = new EmailAddress(_senderEmail, _senderName);
            var subject = campaign.Name;
            var plainTextContent = campaign.Description;
            var htmlContent = $"<h2>{campaign.Name}</h2><p>{campaign.Description}</p>";

            foreach (var recipientEmail in recipients)
            {
                var to = new EmailAddress(recipientEmail);
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                await client.SendEmailAsync(msg);
            }
        }

        public async Task<bool> SendPasswordResetEmailAsync(string email, string resetLink)
        {
            try
            {
                var client = new SendGridClient(_apiKey);
                var from = new EmailAddress(_senderEmail, _senderName);
                var to = new EmailAddress(email);
                var subject = "Password Reset Request";
                var plainTextContent = $"Click the link to reset your password: {resetLink}";
                var htmlContent = $@"
                    <h2>Password Reset</h2>
                    <p>Click the link below to reset your password:</p>
                    <a href='{resetLink}'>Reset Password</a>
                ";
                
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await client.SendEmailAsync(msg);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }
    }
}
