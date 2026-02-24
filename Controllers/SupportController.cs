using ClientSphere.ViewModels;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class SupportController : Controller
    {
        private readonly ISupportService _supportService;

        public SupportController(ISupportService supportService)
        {
            _supportService = supportService;
        }

        public async Task<IActionResult> Index(bool archived = false)
        {
            var allTickets = await _supportService.GetAllTicketsAsync();
            var tickets = archived ? allTickets.Where(t => t.Status == "Closed" || t.Status == "Resolved") : allTickets.Where(t => t.Status != "Closed" && t.Status != "Resolved");
            ViewData["IsArchived"] = archived;
            
            var viewModel = new SupportDashboardViewModel
            {
                TotalTickets = tickets.Count(),
                InProgressTickets = tickets.Count(t => t.Status == "In Progress"),
                ResolvedTickets = tickets.Count(t => t.Status == "Resolved" || t.Status == "Closed"),
                CriticalTickets = tickets.Count(t => t.Priority == "Critical" || t.Priority == "High"),
                Tickets = tickets.Select(t => new TicketViewModel
                {
                    Id = t.Id,
                    TicketId = $"TICK-{t.Id:D3}",
                    Subject = t.Subject,
                    Description = t.Description,
                    Status = t.Status,
                    Priority = t.Priority,
                    CustomerName = $"Customer {t.CustomerId}",
                    CustomerId = t.CustomerId,
                    AssignedTo = "Support Team",
                    CreatedAt = t.CreatedAt,
                    LastUpdated = t.LastUpdated ?? t.CreatedAt
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Models.SupportTicket ticket)
        {
            if (ModelState.IsValid)
            {
                // For now, assign a default customer ID if not provided, or handle it in the service
                if (string.IsNullOrEmpty(ticket.CustomerId))
                {
                    ticket.CustomerId = "ADMIN-CREATED";
                }
                
                ticket.Status = "Open";
                ticket.CreatedAt = DateTime.UtcNow;
                ticket.LastUpdated = DateTime.UtcNow;
                
                await _supportService.CreateTicketAsync(ticket);
                return RedirectToAction(nameof(Index));
            }
            return View(ticket);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var ticket = await _supportService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            return View(ticket);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.SupportTicket ticket)
        {
            if (id != ticket.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _supportService.UpdateTicketAsync(ticket);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    if (await _supportService.GetTicketByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(ticket);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _supportService.GetTicketByIdAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }
            return View(ticket);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _supportService.DeleteTicketAsync(id);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int ticketId, string comment)
        {
            var ticket = await _supportService.GetTicketByIdAsync(ticketId);
            if (ticket != null && !string.IsNullOrWhiteSpace(comment))
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm");
                var newNote = $"\n\n[Comment by Staff – {timestamp}]: {comment}";
                ticket.Description = (ticket.Description ?? "") + newNote;
                ticket.LastUpdated = DateTime.UtcNow;
                await _supportService.UpdateTicketAsync(ticket);
                TempData["Success"] = "Comment added successfully.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CloseTicket(int id)
        {
            var ticket = await _supportService.GetTicketByIdAsync(id);
            if (ticket != null)
            {
                ticket.Status = "Closed";
                ticket.LastUpdated = DateTime.UtcNow;
                await _supportService.UpdateTicketAsync(ticket);
                TempData["Success"] = "Ticket marked as Closed.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
