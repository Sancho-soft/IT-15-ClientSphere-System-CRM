using ClientSphere.ViewModels;
using ClientSphere.Services;
using ClientSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Support Staff,Admin,Super Admin")]
    public class SupportStaffController : Controller
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public SupportStaffController(Data.ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            // Fetch real tickets
            var allTickets = await _context.SupportTickets.ToListAsync();
            
            // In a real app we would Join with Users table to get Customer Name, 
            // but for now we might have to load it or just use Email if stored.
            // SupportTicket has CustomerId (string).
            
            var openTickets = allTickets.Where(t => t.Status != "Resolved" && t.Status != "Closed").OrderByDescending(t => t.Priority).ToList();
            var resolvedToday = allTickets.Count(t => (t.Status == "Resolved" || t.Status == "Closed") && t.LastUpdated.HasValue && t.LastUpdated.Value.Date == DateTime.UtcNow.Date);
            
            // Map to ViewModel
            var ticketViewModels = new List<TicketViewModel>();
            foreach(var t in openTickets)
            {
                var customer = await _userManager.FindByIdAsync(t.CustomerId);
                ticketViewModels.Add(new TicketViewModel
                {
                    TicketId = "TICK-" + t.Id.ToString("D3"),
                    Subject = t.Subject,
                    Status = t.Status,
                    Priority = t.Priority,
                    CustomerName = customer != null ? $"{customer.FirstName} {customer.LastName}" : "Unknown",
                    LastUpdated = t.LastUpdated ?? t.CreatedAt,
                    Id = t.Id // Add simple Id for linking
                });
            }

            // Calculate Average Resolution Time
            var resolvedTickets = allTickets.Where(t => t.Status == "Resolved" || t.Status == "Closed").ToList();
            double avgHours = 0;
            if (resolvedTickets.Any())
            {
                avgHours = resolvedTickets.Average(t => 
                {
                    var end = t.LastUpdated ?? DateTime.UtcNow;
                    return (end - t.CreatedAt).TotalHours;
                });
            }

            var viewModel = new SupportStaffViewModel
            {
                MyOpenTickets = openTickets.Count,
                MyResolvedToday = resolvedToday,
                AvgResolutionTimeHours = (int)Math.Round(avgHours),
                MyActiveTickets = ticketViewModels
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.Status = "Resolved";
                ticket.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var ticket = await _context.SupportTickets.FindAsync(id);
            if (ticket != null)
            {
                ticket.Status = "Closed";
                ticket.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Ticket #{id} has been marked as Closed.";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ticketId, string comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] = "Comment cannot be empty.";
                return RedirectToAction(nameof(Dashboard));
            }

            var ticket = await _context.SupportTickets.FindAsync(ticketId);
            if (ticket != null)
            {
                var user = await _userManager.GetUserAsync(User);
                var userName = user != null ? $"{user.FirstName} {user.LastName}".Trim() : User.Identity?.Name ?? "Staff";
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var newNote = $"\n\n--- Comment by {userName} at {timestamp} ---\n{comment}";
                ticket.Description = (ticket.Description ?? "") + newNote;
                ticket.LastUpdated = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Comment added successfully.";
            }
            return RedirectToAction(nameof(Dashboard));
        }
    }
}
