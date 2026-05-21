using ClientSphere.ViewModels;
using ClientSphere.Models;
using ClientSphere.Data;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Customer, Admin, Super Admin")]
    public class CustomerPortalController : Controller
    {
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";
        private const string CurrentPageKey = "CurrentPage";

        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISupportService _supportService;
        private readonly IPaymongoService _paymongoService;
        private readonly ICloudinaryService _cloudinaryService;

        public CustomerPortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISupportService supportService, IPaymongoService paymongoService, ICloudinaryService cloudinaryService)
        {
            _context = context;
            _userManager = userManager;
            _supportService = supportService;
            _paymongoService = paymongoService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var tickets = await _supportService.GetTicketsByCustomerIdAsync(userId);
            
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            var realOrders = new List<Order>();
            if (customer != null)
            {
                 realOrders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            var viewModel = new CustomerDashboardViewModel
            {
                TotalOrders = realOrders.Count,
                ActiveTickets = tickets.Count(t => t.Status != "Closed" && t.Status != "Resolved"),
                TotalSpent = realOrders.Where(o => o.Status.ToString() != "Cancelled").Sum(o => o.TotalAmount),
                RecentOrders = realOrders.Take(5).Select(o => new CustomerOrderViewModel 
                { 
                    OrderId = o.Id.ToString(), 
                    OrderDate = o.OrderDate, 
                    Status = o.Status.ToString(), 
                    TotalAmount = o.TotalAmount, 
                    ItemCount = o.OrderItems.Count 
                }).ToList(),
                RecentTickets = tickets.Take(5).Select(t => new TicketViewModel
                {
                    TicketId = t.Id.ToString(),
                    Subject = t.Subject,
                    Status = t.Status,
                    Priority = t.Priority,
                    LastUpdated = t.LastUpdated ?? t.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult CreateTicket()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTicket(string subject, string description)
        {
            if (!ModelState.IsValid)
            {
                return View();
            }

            if (string.IsNullOrWhiteSpace(subject) || string.IsNullOrWhiteSpace(description))
            {
                ModelState.AddModelError("", "Subject and description are required.");
                return View();
            }

            var userId = _userManager.GetUserId(User);
            var ticket = new SupportTicket
            {
                Subject = subject,
                Description = description,
                CustomerId = userId,
                Status = "Open",
                Priority = "Medium",
                CreatedAt = DateTime.UtcNow,
                LastUpdated = DateTime.UtcNow
            };

            await _supportService.CreateTicketAsync(ticket);

            TempData[SuccessMessageKey] = "Your support ticket has been submitted successfully.";
            return RedirectToAction(nameof(Dashboard));
        }

        // GET: CustomerPortal/MyOrders
        public async Task<IActionResult> MyOrders()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            var orders = new List<Order>();
            if (customer != null)
            {
                orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToListAsync();
            }

            ViewData[CurrentPageKey] = "My Orders";
            return View(orders);
        }

        // GET: CustomerPortal/MyTickets
        public async Task<IActionResult> MyTickets()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var tickets = await _supportService.GetTicketsByCustomerIdAsync(userId);
            
            ViewData[CurrentPageKey] = "Support Tickets";
            return View(tickets);
        }

        // GET: CustomerPortal/MyInvoices
        public async Task<IActionResult> MyInvoices()
        {
            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            var invoices = new List<Invoice>();
            if (customer != null)
            {
                invoices = await _context.Invoices
                    .Where(i => i.CustomerId == customer.Id)
                    .OrderByDescending(i => i.IssueDate)
                    .ToListAsync();
            }

            ViewData[CurrentPageKey] = "Invoices";
            return View(invoices);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveTicket(int id)
        {
            if (!ModelState.IsValid) return BadRequest();

            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return NotFound();

            var ticket = await _supportService.GetTicketByIdAsync(id);
            if (ticket != null && ticket.CustomerId == userId)
            {
                ticket.Status = "Closed";
                ticket.LastUpdated = DateTime.UtcNow;
                await _supportService.UpdateTicketAsync(ticket);
                TempData[SuccessMessageKey] = "Ticket successfully archived (Closed).";
            }
            return RedirectToAction(nameof(MyTickets));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveInvoice(int id)
        {
            if (!ModelState.IsValid) return BadRequest();

            var userId = _userManager.GetUserId(User);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
            if (customer == null) return NotFound();

            var invoice = await _context.Invoices.FindAsync(id);
            // Simulate archiving an invoice by marking it as Paid/Cancelled in this demo
            if (invoice != null && invoice.CustomerId == customer.Id)
            {
                invoice.Status = "Paid"; 
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = "Invoice successfully archived (Paid)."; 
            }
            return RedirectToAction(nameof(MyInvoices));
        }

        // POST: CustomerPortal/PayWithPaymongo
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayWithPaymongo(int id)
        {
            if (!ModelState.IsValid) return BadRequest();

            try
            {
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
                if (customer == null) return NotFound();

                var invoice = await _context.Invoices
                    .Include(i => i.Customer)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CustomerId == customer.Id);

                if (invoice == null) return NotFound();

                // Only allow payment of non-paid invoices
                if (invoice.Status == "Paid" || invoice.Status == "Cancelled")
                {
                    TempData[ErrorMessageKey] = "This invoice has already been settled.";
                    return RedirectToAction(nameof(MyInvoices));
                }

                // Build a success URL that carries the invoice ID so PaymentSuccess can update the DB
                var baseSuccessUrl = Url.Action(nameof(PaymentSuccess), "CustomerPortal", null, Request.Scheme);
                var customSuccessUrl = $"{baseSuccessUrl}?invoiceId={invoice.Id}";

                var paymentUrl = await _paymongoService.CreatePaymentLinkAsync(invoice, customSuccessUrl);
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData[ErrorMessageKey] = $"Payment could not be initiated: {ex.Message}";
                return RedirectToAction(nameof(MyInvoices));
            }
        }

        // GET: CustomerPortal/PaymentSuccess
        public async Task<IActionResult> PaymentSuccess(int? invoiceId)
        {
            if (!ModelState.IsValid) return BadRequest();

            if (invoiceId.HasValue)
            {
                var userId = _userManager.GetUserId(User);
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

                if (customer != null)
                {
                    var invoice = await _context.Invoices
                        .FirstOrDefaultAsync(i => i.Id == invoiceId.Value && i.CustomerId == customer.Id);

                    if (invoice != null && invoice.Status != "Paid")
                    {
                        // Set to Processing — billing staff will review and confirm as Paid
                        invoice.Status = "Processing";
                        if (string.IsNullOrEmpty(invoice.PaymentMethod) || invoice.PaymentMethod == "PayMongo")
                        {
                            invoice.PaymentMethod = "PayMongo";
                        }
                        await _context.SaveChangesAsync();
                    }
                }
            }

            TempData[SuccessMessageKey] = "Payment submitted! Your invoice is being reviewed by our billing team.";
            return RedirectToAction(nameof(MyInvoices));
        }

        // GET: CustomerPortal/PaymentCancelled
        public IActionResult PaymentCancelled()
        {
            TempData[ErrorMessageKey] = "Payment was cancelled. Please try again if needed.";
            return RedirectToAction(nameof(MyInvoices));
        }

        // GET: CustomerPortal/MyProfile
        public async Task<IActionResult> MyProfile()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }
            
            ViewData[CurrentPageKey] = "My Profile";
            return View(user);
        }

        // POST: CustomerPortal/MyProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(string firstName, string lastName, string phoneNumber, IFormFile? profilePicture)
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            if (!ModelState.IsValid)
            {
                ViewData[CurrentPageKey] = "My Profile";
                return View(user);
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.PhoneNumber = phoneNumber;

            if (profilePicture != null && profilePicture.Length > 0)
            {
                if (!ClientSphere.Helpers.FileUploadValidator.IsValidImageType(profilePicture))
                {
                    TempData[ErrorMessageKey] = "Only image files (JPEG, PNG, GIF, WebP) are allowed.";
                    return RedirectToAction(nameof(MyProfile));
                }
                if (!ClientSphere.Helpers.FileUploadValidator.IsWithinSizeLimit(profilePicture))
                {
                    TempData[ErrorMessageKey] = "Profile picture must not exceed 5 MB.";
                    return RedirectToAction(nameof(MyProfile));
                }
                try
                {
                    string? imageUrl = await _cloudinaryService.UploadImageAsync(profilePicture, "profile_pictures");
                    if (!string.IsNullOrEmpty(imageUrl))
                        user.ProfilePictureUrl = imageUrl;
                }
                catch (Exception ex)
                {
                    TempData[ErrorMessageKey] = "Failed to upload profile picture: " + ex.Message;
                    return RedirectToAction(nameof(MyProfile));
                }
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData[SuccessMessageKey] = "Profile updated successfully!";
                return RedirectToAction(nameof(MyProfile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewData[CurrentPageKey] = "My Profile";
            return View(user);
        }

        [HttpGet]
        public async Task<IActionResult> GetChatbotSummary()
        {
            var userId = _userManager.GetUserId(User);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var tickets = await _supportService.GetTicketsByCustomerIdAsync(userId);
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);

            var orders = new List<Order>();
            var invoices = new List<Invoice>();
            if (customer != null)
            {
                orders = await _context.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .ToListAsync();

                invoices = await _context.Invoices
                    .Where(i => i.CustomerId == customer.Id)
                    .OrderByDescending(i => i.IssueDate)
                    .Take(5)
                    .ToListAsync();
            }

            return Json(new
            {
                tickets = tickets.Select(t => new { id = t.Id, subject = t.Subject, status = t.Status, priority = t.Priority }),
                orders = orders.Select(o => new { id = o.Id, orderDate = o.OrderDate.ToString("yyyy-MM-dd"), status = o.Status.ToString(), totalAmount = o.TotalAmount }),
                invoices = invoices.Select(i => new { id = i.Id, issueDate = i.IssueDate.ToString("yyyy-MM-dd"), status = i.Status, totalAmount = i.Amount })
            });
        }
    }
}
