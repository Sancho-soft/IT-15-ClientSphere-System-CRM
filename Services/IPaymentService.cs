using System.Threading.Tasks;

namespace ClientSphere.Services
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentLinkAsync(int invoiceId);
        Task<bool> ProcessWebhookAsync(string payload, string signature);
        Task<string> GetPaymentStatusAsync(string paymentIntentId);
    }
}
