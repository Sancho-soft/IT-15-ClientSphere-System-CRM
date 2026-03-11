using ClientSphere.Data;
using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Billing Staff,Admin,Super Admin")]
    public class BillingStaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IInvoiceService _invoiceService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BillingStaffController(ApplicationDbContext context, IInvoiceService invoiceService, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _invoiceService = invoiceService;
            _userManager = userManager;
        }

        // GET: BillingStaff/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var allInvoices = await _context.Invoices.Include(i => i.Customer).ToListAsync();

            var now = DateTime.UtcNow;
            var startOfMonth = new DateTime(now.Year, now.Month, 1);

            ViewBag.TotalInvoices = allInvoices.Count(i => !i.IsArchived);
            ViewBag.ProcessingCount = allInvoices.Count(i => i.Status == "Processing" && !i.IsArchived);
            ViewBag.PaidThisMonth = allInvoices
                .Where(i => i.Status == "Paid" && i.PaidDate.HasValue && i.PaidDate.Value >= startOfMonth)
                .Sum(i => i.Amount);
            ViewBag.OverdueCount = allInvoices.Count(i => i.Status == "Overdue" && !i.IsArchived);
            ViewBag.TotalPaidRevenue = allInvoices.Where(i => i.Status == "Paid").Sum(i => i.Amount);

            // Processing invoices — need confirmation
            var processingInvoices = allInvoices
                .Where(i => i.Status == "Processing" && !i.IsArchived)
                .OrderByDescending(i => i.IssueDate)
                .ToList();

            ViewData["Title"] = "Billing Dashboard";
            ViewData["HideTopNavTitle"] = true;
            ViewData["CurrentPage"] = "Dashboard";

            return View(processingInvoices);
        }

        // GET: BillingStaff/PaymentHistory
        public async Task<IActionResult> PaymentHistory(string? search, string? method, string? dateFrom, string? dateTo)
        {
            var records = await _context.PaymentRecords
                .Include(p => p.Invoice)
                .OrderByDescending(p => p.PaidAt)
                .ToListAsync();

            // Filter
            if (!string.IsNullOrEmpty(search))
            {
                search = search.Trim().ToLower();
                records = records.Where(r =>
                    r.InvoiceNumber.ToLower().Contains(search) ||
                    r.CustomerName.ToLower().Contains(search) ||
                    (r.TransactionId?.ToLower().Contains(search) ?? false)
                ).ToList();
            }
            if (!string.IsNullOrEmpty(method))
            {
                records = records.Where(r => r.PaymentMethod != null && r.PaymentMethod.Contains(method, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            if (DateTime.TryParse(dateFrom, out var from))
            {
                records = records.Where(r => r.PaidAt.Date >= from.Date).ToList();
            }
            if (DateTime.TryParse(dateTo, out var to))
            {
                records = records.Where(r => r.PaidAt.Date <= to.Date).ToList();
            }

            ViewBag.Search = search;
            ViewBag.Method = method;
            ViewBag.DateFrom = dateFrom;
            ViewBag.DateTo = dateTo;
            ViewBag.TotalPaid = records.Sum(r => r.AmountPaid);

            ViewData["Title"] = "Payment History";
            ViewData["HideTopNavTitle"] = true;
            ViewData["CurrentPage"] = "PaymentHistory";

            return View(records);
        }
    }
}
