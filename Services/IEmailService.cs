using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface IEmailService
    {
        Task SendWelcomeEmailAsync(string email, string name);
        Task SendInvoiceEmailAsync(string email, Invoice invoice);
        Task SendCampaignEmailAsync(List<string> recipients, Campaign campaign);
        Task<bool> SendPasswordResetEmailAsync(string email, string resetLink);
    }
}
