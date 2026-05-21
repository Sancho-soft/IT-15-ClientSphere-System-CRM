using ClientSphere.ViewModels;
using ClientSphere.Services;
using ClientSphere.Models;
using ClientSphere.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Sales Manager,Billing Staff")]
    public class BillingController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IEmailService _emailService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IPaymongoService _paymongoService;
        private readonly ApplicationDbContext _context;
        private readonly INotificationService _notificationService;

        public BillingController(IInvoiceService invoiceService, IEmailService emailService, ICustomerService customerService, IOrderService orderService, UserManager<ApplicationUser> userManager, IPaymongoService paymongoService, ApplicationDbContext context, INotificationService notificationService)
        {
            _invoiceService = invoiceService;
            _emailService = emailService;
            _customerService = customerService;
            _orderService = orderService;
            _userManager = userManager;
            _paymongoService = paymongoService;
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<IActionResult> Index(bool archived = false)
        {
            var allInvoices = await _invoiceService.GetAllInvoicesAsync();
            var filteredInvoices = allInvoices.Where(i => i.IsArchived == archived).ToList();
            var stats = await _invoiceService.GetFinancialStatsAsync();

            decimal totalPaid = 0;
            decimal pending = 0;
            decimal overdue = 0;

            if (stats.ContainsKey("PaidRevenue")) totalPaid = stats["PaidRevenue"];
            if (stats.ContainsKey("PendingRevenue")) pending = stats["PendingRevenue"];
            if (stats.ContainsKey("OverdueRevenue")) overdue = stats["OverdueRevenue"];

            // Exchange rate API removed; hardcoding to 1 for native PHP pricing
            decimal currentPhpRate = 1m;

            var viewModel = new BillingDashboardViewModel
            {
                TotalRevenue = totalPaid,
                PendingRevenue = pending,
                OverdueRevenue = overdue,
                TotalInvoices = filteredInvoices.Count,
                Invoices = filteredInvoices.Select(i => new InvoiceViewModel
                {
                    Id = i.Id,
                    InvoiceId = i.InvoiceNumber,
                    IssuedDate = i.IssueDate,
                    DueDate = i.DueDate,
                    CustomerName = i.Customer?.ContactName ?? "Unknown",
                    CustomerId = $"CUST-{i.CustomerId}",
                    SaleId = $"SALE-{i.OrderId}",
                    Amount = i.Amount,
                    Status = i.Status,
                    PaymentMethod = i.PaymentMethod,
                    ExchangeRate = currentPhpRate
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> ExportCsv(bool archived = false)
        {
            var allInvoices = await _invoiceService.GetAllInvoicesAsync();
            var filteredInvoices = allInvoices.Where(i => i.IsArchived == archived).ToList();

            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Invoice ID,Issue Date,Due Date,Customer,Sale ID,Status,Payment Method,Subtotal,VAT (12%),Total");

            foreach (var invoice in filteredInvoices)
            {
                var customerName = invoice.Customer?.ContactName ?? "Unknown";
                var vat = invoice.Amount * 0.12m;
                var total = invoice.Amount + vat;
                
                // Escape quotes if customer name has commas
                var safeCustomerName = customerName.Replace("\"", "\"\"");
                
                sb.AppendLine($"{invoice.InvoiceNumber},{invoice.IssueDate:yyyy-MM-dd},{invoice.DueDate:yyyy-MM-dd},\"{safeCustomerName}\",{invoice.OrderId},{invoice.Status},{invoice.PaymentMethod},{invoice.Amount:0.00},{vat:0.00},{total:0.00}");
            }

            var fileBytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            return File(fileBytes, "text/csv", $"ClientSphere_Invoices_{DateTime.Now:yyyyMMdd}.csv");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Billing Staff")]
        public async Task<IActionResult> ArchiveInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice != null)
            {
                invoice.IsArchived = true;
                await _invoiceService.UpdateInvoiceAsync(invoice);
                TempData["Success"] = "Invoice archived successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Super Admin,Admin,Billing Staff")]
        public async Task<IActionResult> UnarchiveInvoice(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice != null)
            {
                invoice.IsArchived = false;
                await _invoiceService.UpdateInvoiceAsync(invoice);
                TempData["Success"] = "Invoice restored successfully.";
            }
            return RedirectToAction(nameof(Index), new { archived = true });
        }

        public async Task<IActionResult> Create()
        {
            // Auto-sync Customer role users into Customers table
            var customerUsers = await _userManager.GetUsersInRoleAsync("Customer");
            var existingCustomers = await _customerService.GetAllCustomersAsync();
            foreach (var user in customerUsers)
            {
                if (!existingCustomers.Any(c => c.Email == user.Email))
                {
                    await _customerService.CreateCustomerAsync(new Customer
                    {
                        CompanyName = $"{user.FirstName} {user.LastName}",
                        ContactName = $"{user.FirstName} {user.LastName}",
                        Email = user.Email ?? "",
                        Phone = user.PhoneNumber ?? "",
                        IsActive = true
                    });
                }
            }

            var customers = await _customerService.GetAllCustomersAsync();
            var orders = await _orderService.GetAllOrdersAsync();
            ViewBag.Customers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(customers, "Id", "CompanyName");
            ViewBag.Orders = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(orders.Select(o => new { o.Id, DisplayText = $"ORD-{o.Id:D3} - ₱{o.TotalAmount:N0}"}), "Id", "DisplayText");
            
            // Generate a default invoice number
            var model = new Invoice
            {
                InvoiceNumber = $"INV-{DateTime.Now.Year}-{new Random().Next(1000, 9999)}",
                IssueDate = DateTime.Today,
                DueDate = DateTime.Today.AddDays(14),
                Status = "Unpaid"
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Invoice invoice)
        {
            // Remove navigation property from validation (it's not posted from form)
            ModelState.Remove("Customer");
            ModelState.Remove("customer");

            if (ModelState.IsValid)
            {
                if (invoice.CreatedAt == default)
                    invoice.CreatedAt = DateTime.UtcNow;

                await _invoiceService.CreateInvoiceAsync(invoice);
                TempData["Success"] = "Invoice created successfully.";
                return RedirectToAction(nameof(Index));
            }

            var customers = await _customerService.GetAllCustomersAsync();
            var orders = await _orderService.GetAllOrdersAsync();
            ViewBag.Customers = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(customers, "Id", "CompanyName");
            ViewBag.Orders = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(orders.Select(o => new { o.Id, DisplayText = $"ORD-{o.Id:D3} - ₱{o.TotalAmount:N0}"}), "Id", "DisplayText");

            return View(invoice);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Invoice invoice)
        {
            if (id != invoice.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                    return RedirectToAction(nameof(Index));
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
                {
                    if (await _invoiceService.GetInvoiceByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsPaid(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice != null)
            {
                var wasProcessing = invoice.Status == "Processing";
                invoice.Status = "Paid";
                if (string.IsNullOrEmpty(invoice.PaymentMethod) || invoice.PaymentMethod == "PayMongo")
                    invoice.PaymentMethod = "Manual";
                invoice.PaidDate = DateTime.UtcNow;
                await _invoiceService.UpdateInvoiceAsync(invoice);

                // Create Payment History record
                var customerName = invoice.Customer?.ContactName ?? "Unknown";
                
                try
                {
                    var paymentRecord = new PaymentRecord
                    {
                        InvoiceId = invoice.Id,
                        InvoiceNumber = invoice.InvoiceNumber ?? $"INV-{invoice.Id}",
                        CustomerName = customerName,
                        PaymentMethod = invoice.PaymentMethod,
                        TransactionId = invoice.TransactionId,
                        AmountPaid = invoice.Amount,
                        PaidAt = DateTime.UtcNow
                    };
                    
                    _context.PaymentRecords.Add(paymentRecord);
                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    // Log error but don't stop the customer notification
                    Console.WriteLine($"Error saving PaymentRecord: {ex.Message}");
                }

                // Notify the customer that their invoice was confirmed as Paid
                if (invoice.Customer != null)
                {
                    var customerUser = await _userManager.FindByEmailAsync(invoice.Customer.Email);
                    if (customerUser != null)
                    {
                        await _notificationService.CreateForUserAsync(
                            customerUser.Id,
                            "Invoice Confirmed as Paid",
                            $"Your invoice {invoice.InvoiceNumber} for &#8369;{invoice.Amount:N2} has been confirmed as paid.",
                            "/CustomerPortal/MyInvoices",
                            "Billing"
                        );
                    }
                }
            }
            TempData["Success"] = "Invoice marked as paid.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePaymongoLink(int id)
        {
            try
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
                if (invoice == null) return NotFound();

                var paymentUrl = await _paymongoService.CreatePaymentLinkAsync(invoice);
                // Embed invoice ID in success URL so PaymentSuccess can update the DB
                var baseSuccessUrl = Url.Action(nameof(PaymentSuccess), "Billing", null, Request.Scheme);
                var customSuccessUrl = $"{baseSuccessUrl}?invoiceId={invoice.Id}";
                paymentUrl = await _paymongoService.CreatePaymentLinkAsync(invoice, customSuccessUrl);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to generate Paymongo link: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        public async Task<IActionResult> PaymentSuccess(int? invoiceId)
        {
            if (invoiceId.HasValue)
            {
                var invoice = await _invoiceService.GetInvoiceByIdAsync(invoiceId.Value);
                if (invoice != null && invoice.Status != "Paid")
                {
                    // Set to Processing — billing staff must confirm before marking as Paid
                    invoice.Status = "Processing";
                    if (string.IsNullOrEmpty(invoice.PaymentMethod) || invoice.PaymentMethod == "PayMongo")
                    {
                        invoice.PaymentMethod = "PayMongo";
                    }
                    await _invoiceService.UpdateInvoiceAsync(invoice);
                }
            }
            TempData["Success"] = "Payment submitted! Billing staff will verify and confirm your payment.";
            return RedirectToAction(nameof(Index));
        }

        public IActionResult PaymentCancelled()
        {
            TempData["Error"] = "Payment was cancelled.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendInvoiceEmail(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice?.Customer != null)
            {
                await _emailService.SendInvoiceEmailAsync(invoice.Customer.Email, invoice);
                TempData["Success"] = "Invoice email sent successfully!";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
