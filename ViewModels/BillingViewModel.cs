using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class BillingDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public decimal OverdueRevenue { get; set; }
        public int TotalInvoices { get; set; }
        public List<InvoiceViewModel> Invoices { get; set; } = new List<InvoiceViewModel>();
    }

    public class InvoiceViewModel
    {
        public int Id { get; set; } // Database ID
        public string InvoiceId { get; set; } = string.Empty;
        public DateTime IssuedDate { get; set; }
        public DateTime DueDate { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string SaleId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Status { get; set; } = "Unpaid"; // Paid, Unpaid, Overdue
        public string PaymentMethod { get; set; } = "-";
    }
}
