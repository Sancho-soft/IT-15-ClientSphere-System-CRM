using ClientSphere.ViewModels;
using ClientSphere.Services;
using ClientSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class BillingController : Controller
    {
        private readonly IInvoiceService _invoiceService;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly ICustomerService _customerService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;

        public BillingController(IInvoiceService invoiceService, IPaymentService paymentService, IEmailService emailService, ICustomerService customerService, IOrderService orderService, UserManager<ApplicationUser> userManager)
        {
            _invoiceService = invoiceService;
            _paymentService = paymentService;
            _emailService = emailService;
            _customerService = customerService;
            _orderService = orderService;
            _userManager = userManager;
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
                    PaymentMethod = i.PaymentMethod
                }).ToList()
            };

            return View(viewModel);
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
                Status = "Draft"
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
                catch (Exception)
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
                invoice.Status = "Paid";
                invoice.PaymentMethod = "Bank Transfer";
                invoice.PaidDate = DateTime.UtcNow;
                await _invoiceService.UpdateInvoiceAsync(invoice);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GeneratePaymentLink(int id)
        {
            try
            {
                var paymentUrl = await _paymentService.CreatePaymentLinkAsync(id);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Failed to generate payment link: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"].ToString();

            var success = await _paymentService.ProcessWebhookAsync(json, signature);
            return success ? Ok() : BadRequest();
        }

        public IActionResult PaymentSuccess()
        {
            TempData["Success"] = "Payment completed successfully!";
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
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var invoice = await _invoiceService.GetInvoiceByIdAsync(id);
            if (invoice == null)
            {
                return NotFound();
            }
            return View(invoice);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _invoiceService.DeleteInvoiceAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
