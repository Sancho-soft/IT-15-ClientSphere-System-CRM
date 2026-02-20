using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Services
{
    public class InvoiceService : IInvoiceService
    {
        private readonly ApplicationDbContext _context;

        public InvoiceService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Invoice>> GetAllInvoicesAsync()
        {
            return await _context.Invoices.Include(i => i.Customer).OrderByDescending(i => i.IssueDate).ToListAsync();
        }

        public async Task<List<Invoice>> GetInvoicesByCustomerAsync(int customerId)
        {
            return await _context.Invoices.Include(i => i.Customer)
                                          .Where(i => i.CustomerId == customerId)
                                          .OrderByDescending(i => i.IssueDate)
                                          .ToListAsync();
        }

        public async Task<Invoice?> GetInvoiceByIdAsync(int id)
        {
            return await _context.Invoices.Include(i => i.Customer).FirstOrDefaultAsync(i => i.Id == id);
        }

        public async Task CreateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Add(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateInvoiceAsync(Invoice invoice)
        {
            _context.Invoices.Update(invoice);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteInvoiceAsync(int id)
        {
            var invoice = await _context.Invoices.FindAsync(id);
            if (invoice != null)
            {
                _context.Invoices.Remove(invoice);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<string, decimal>> GetFinancialStatsAsync()
        {
            var stats = new Dictionary<string, decimal>
            {
                { "TotalRevenue", await _context.Invoices.SumAsync(i => i.Amount) }, // Total invoiced
                { "PendingRevenue", await _context.Invoices.Where(i => i.Status == "Sent" || i.Status == "Pending").SumAsync(i => i.Amount) },
                { "PaidRevenue", await _context.Invoices.Where(i => i.Status == "Paid").SumAsync(i => i.Amount) },
                { "OverdueRevenue", await _context.Invoices.Where(i => i.Status == "Overdue").SumAsync(i => i.Amount) }
            };
            return stats;
        }
    }
}
