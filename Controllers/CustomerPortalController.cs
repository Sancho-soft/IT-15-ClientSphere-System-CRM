using ClientSphere.ViewModels;
using ClientSphere.Models;
using ClientSphere.Data;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Customer, Admin")]
    public class CustomerPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ISupportService _supportService;

        public CustomerPortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, ISupportService supportService)
        {
            _context = context;
            _userManager = userManager;
            _supportService = supportService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var tickets = await _supportService.GetTicketsByCustomerIdAsync(userId);
            
            var userName = User.Identity?.Name;
            
            // Find the Customer entity linked to this user's email
            // Note: In a production app, we should link ApplicationUser.Id to Customer.UserId more explicitly,
            // but for now, matching by Email is a safe fallback given the DbInitializer logic.
            var customer = !string.IsNullOrEmpty(userName) 
                ? _context.Customers.FirstOrDefault(c => c.Email == userName) 
                : null;

            var realOrders = new List<Order>();
            if (customer != null)
            {
                 realOrders = _context.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }

            var viewModel = new CustomerDashboardViewModel
            {
                TotalOrders = realOrders.Count,
                ActiveTickets = tickets.Count(t => t.Status != "Closed" && t.Status != "Resolved"),
                TotalSpent = realOrders.Sum(o => o.TotalAmount),
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
        public async Task<IActionResult> CreateTicket(SupportTicket ticket)
        {
            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                ticket.CustomerId = userId;
                ticket.Status = "Open";
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.LastUpdated = DateTime.UtcNow;

                await _supportService.CreateTicketAsync(ticket);

                return RedirectToAction(nameof(Dashboard));
            }
            return View(ticket);
        }

        // GET: CustomerPortal/MyOrders
        public IActionResult MyOrders()
        {
            var userName = User.Identity?.Name;
            var customer = !string.IsNullOrEmpty(userName) 
                ? _context.Customers.FirstOrDefault(c => c.Email == userName) 
                : null;

            var orders = new List<Order>();
            if (customer != null)
            {
                orders = _context.Orders
                    .Where(o => o.CustomerId == customer.Id)
                    .OrderByDescending(o => o.OrderDate)
                    .ToList();
            }

            ViewData["CurrentPage"] = "My Orders";
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
            
            ViewData["CurrentPage"] = "Support Tickets";
            return View(tickets);
        }

        // GET: CustomerPortal/MyInvoices
        public IActionResult MyInvoices()
        {
            var userName = User.Identity?.Name;
            var customer = !string.IsNullOrEmpty(userName) 
                ? _context.Customers.FirstOrDefault(c => c.Email == userName) 
                : null;

            var invoices = new List<Invoice>();
            if (customer != null)
            {
                invoices = _context.Invoices
                    .Where(i => i.CustomerId == customer.Id)
                    .OrderByDescending(i => i.IssueDate)
                    .ToList();
            }

            ViewData["CurrentPage"] = "Invoices";
            return View(invoices);
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
            
            ViewData["CurrentPage"] = "My Profile";
            return View(user);
        }

        // POST: CustomerPortal/MyProfile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MyProfile(string firstName, string lastName, string email, string phoneNumber)
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

            user.FirstName = firstName;
            user.LastName = lastName;
            user.Email = email;
            user.PhoneNumber = phoneNumber;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Profile updated successfully!";
                return RedirectToAction(nameof(MyProfile));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            return View(user);
        }
    }
}
