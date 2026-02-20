using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface IInvoiceService
    {
        Task<List<Invoice>> GetAllInvoicesAsync();
        Task<List<Invoice>> GetInvoicesByCustomerAsync(int customerId);
        Task<Invoice?> GetInvoiceByIdAsync(int id);
        Task CreateInvoiceAsync(Invoice invoice);
        Task UpdateInvoiceAsync(Invoice invoice);
        Task DeleteInvoiceAsync(int id);
        Task<Dictionary<string, decimal>> GetFinancialStatsAsync();
    }
}
