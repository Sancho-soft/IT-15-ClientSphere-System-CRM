using ClientSphere.ViewModels;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Support Staff")]
    public class SupportController : Controller
    {
        private readonly ISupportService _supportService;
        private readonly ICloudinaryService _cloudinaryService;

        public SupportController(ISupportService supportService, ICloudinaryService cloudinaryService)
        {
            _supportService = supportService;
            _cloudinaryService = cloudinaryService;
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
                    LastUpdated = t.LastUpdated ?? t.CreatedAt,
                    ImageUrl = t.ImageUrl
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
        public async Task<IActionResult> Create(Models.SupportTicket ticket, IFormFile attachment)
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
                
                if (attachment != null && attachment.Length > 0)
                {
                    if (!ClientSphere.Helpers.FileUploadValidator.IsValidImageType(attachment))
                    {
                        TempData["Error"] = "Only image files (JPEG, PNG, GIF, WebP) are allowed.";
                        return View(ticket);
                    }
                    if (!ClientSphere.Helpers.FileUploadValidator.IsWithinSizeLimit(attachment))
                    {
                        TempData["Error"] = "File size must not exceed 5 MB.";
                        return View(ticket);
                    }
                    try
                    {
                        string imageUrl = await _cloudinaryService.UploadImageAsync(attachment, "support_tickets");
                        ticket.ImageUrl = imageUrl;
                    }
                    catch (Exception ex)
                    {
                        TempData["Error"] = $"Image upload failed: {ex.Message}";
                    }
                }

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
