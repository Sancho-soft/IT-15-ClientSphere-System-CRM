using ClientSphere.ViewModels;
using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Added this line

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Sales Staff,Admin")]
    public class SalesStaffController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly Data.ApplicationDbContext _context;
        private readonly ICalendarService _calendarService;

        public SalesStaffController(
            ILeadService leadService, 
            IOpportunityService opportunityService, 
            UserManager<ApplicationUser> userManager, 
            Data.ApplicationDbContext context,
            ICalendarService calendarService)
        {
            _leadService = leadService;
            _opportunityService = opportunityService;
            _userManager = userManager;
            _context = context;
            _calendarService = calendarService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var userId = _userManager.GetUserId(User);
            var leads = await _leadService.GetLeadsBySalesStaffAsync(userId);
            var opportunities = await _opportunityService.GetOpportunitiesBySalesStaffAsync(userId);
            
            // Calculate stats from real data
            var closedDeals = opportunities.Count(o => o.Stage == "Closed Won");
            var pendingLeads = leads.Count(l => l.Status == "New" || l.Status == "Contacted");

            var today = DateTime.UtcNow.Date;
            var wonOpportunities = opportunities.Where(o => o.Stage == "Closed Won").ToList();

            var viewModel = new SalesStaffViewModel
            {
                MyRevenueToday = wonOpportunities
                    .Where(o => o.ExpectedCloseDate.Date == today)
                    .Sum(o => o.EstimatedValue),
                
                MyRevenueMonth = wonOpportunities.Sum(o => o.EstimatedValue),
                MyDealsClosed = closedDeals,
                MyPendingLeads = pendingLeads,
                
                MyRecentSales = wonOpportunities
                    .OrderByDescending(o => o.ExpectedCloseDate)
                    .Take(5)
                    .Select(o => new SalesItemViewModel
                    {
                        CustomerName = o.Name, // Using Opportunity Name as proxy for Customer/Deal Name
                        Date = o.ExpectedCloseDate,
                        Amount = o.EstimatedValue,
                        Status = "Completed"
                    }).ToList()
            };

            return View(viewModel);
        }

        public async Task<IActionResult> MyLeads()
        {
            var userId = _userManager.GetUserId(User);
            var leads = await _leadService.GetLeadsBySalesStaffAsync(userId);
            return View(leads);
        }

        public async Task<IActionResult> MyOpportunities()
        {
            var userId = _userManager.GetUserId(User);
            var opportunities = await _opportunityService.GetOpportunitiesBySalesStaffAsync(userId);
            return View(opportunities);
        }

        public async Task<IActionResult> MyAppointments()
        {
            var userId = _userManager.GetUserId(User);
            var appointments = await _context.Appointments
                .Include(a => a.Customer)
                .Where(a => a.OrganizerUserId == userId)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            return View(appointments);
        }

        // LEADS
        public IActionResult CreateLead()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateLead([Bind("FirstName,LastName,Email,Phone,Company,Source,Status")] Lead lead)
        {
            if (ModelState.IsValid)
            {
                lead.AssignedToUserId = _userManager.GetUserId(User);
                await _leadService.AddLeadAsync(lead);
                TempData["Success"] = "Lead created successfully!";
                return RedirectToAction(nameof(MyLeads));
            }
            return View(lead);
        }

        public async Task<IActionResult> EditLead(int? id)
        {
            if (id == null) return NotFound();
            var lead = await _leadService.GetLeadByIdAsync(id.Value);
            if (lead == null || lead.AssignedToUserId != _userManager.GetUserId(User)) return NotFound();
            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(int id, [Bind("Id,FirstName,LastName,Email,Phone,Company,Source,Status,CreatedAt")] Lead lead)
        {
            if (id != lead.Id) return NotFound();

            if (ModelState.IsValid)
            {
                lead.AssignedToUserId = _userManager.GetUserId(User);
                await _leadService.UpdateLeadAsync(lead);
                TempData["Success"] = "Lead updated successfully!";
                return RedirectToAction(nameof(MyLeads));
            }
            return View(lead);
        }

        // OPPORTUNITIES
        public IActionResult CreateOpportunity()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateOpportunity([Bind("Name,EstimatedValue,Stage,Probability,ExpectedCloseDate")] Opportunity opportunity)
        {
            if (ModelState.IsValid)
            {
                opportunity.AssignedToUserId = _userManager.GetUserId(User);
                await _opportunityService.AddOpportunityAsync(opportunity);
                TempData["Success"] = "Opportunity created successfully!";
                return RedirectToAction(nameof(MyOpportunities));
            }
            return View(opportunity);
        }

        public async Task<IActionResult> EditOpportunity(int? id)
        {
            if (id == null) return NotFound();
            var opportunity = await _opportunityService.GetOpportunityByIdAsync(id.Value);
            if (opportunity == null || opportunity.AssignedToUserId != _userManager.GetUserId(User)) return NotFound();
            return View(opportunity);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOpportunity(int id, [Bind("Id,Name,EstimatedValue,Stage,Probability,ExpectedCloseDate,CreatedAt")] Opportunity opportunity)
        {
            if (id != opportunity.Id) return NotFound();

            if (ModelState.IsValid)
            {
                opportunity.AssignedToUserId = _userManager.GetUserId(User);
                await _opportunityService.UpdateOpportunityAsync(opportunity);
                TempData["Success"] = "Opportunity updated successfully!";
                return RedirectToAction(nameof(MyOpportunities));
            }
            return View(opportunity);
        }

        // APPOINTMENTS
        public IActionResult CreateAppointment()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment([Bind("Title,Description,StartTime,EndTime,Location,Status")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                appointment.OrganizerUserId = _userManager.GetUserId(User);
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment scheduled successfully!";
                return RedirectToAction(nameof(MyAppointments));
            }
            return View(appointment);
        }

        public async Task<IActionResult> EditAppointment(int? id)
        {
            if (id == null) return NotFound();
            var appointment = await _context.Appointments.FindAsync(id.Value);
            if (appointment == null || appointment.OrganizerUserId != _userManager.GetUserId(User)) return NotFound();
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAppointment(int id, [Bind("Id,Title,Description,StartTime,EndTime,Location,Status,CreatedAt,ExternalCalendarId,CustomerId")] Appointment appointment)
        {
            if (id != appointment.Id) return NotFound();

            if (ModelState.IsValid)
            {
                appointment.OrganizerUserId = _userManager.GetUserId(User);
                _context.Appointments.Update(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment updated successfully!";
                return RedirectToAction(nameof(MyAppointments));
            }
            return View(appointment);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SyncToOutlook(int appointmentId)
        {
            var userId = _userManager.GetUserId(User);
            
            // Check if user has access token stored (simplified - in production use secure token storage)
            var accessToken = HttpContext.Session.GetString($"GraphToken_{userId}");
            
            if (string.IsNullOrEmpty(accessToken))
            {
                // Redirect to OAuth flow
                var authUrl = await _calendarService.GetAuthorizationUrlAsync(userId);
                return Redirect(authUrl);
            }

            var appointment = await _context.Appointments.FindAsync(appointmentId);
            if (appointment != null)
            {
                var success = await _calendarService.SyncAppointmentAsync(appointment, accessToken);
                if (success)
                {
                    await _context.SaveChangesAsync();
                    TempData["Success"] = "Appointment synced to Outlook successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to sync appointment to Outlook.";
                }
            }
            
            return RedirectToAction(nameof(MyAppointments));
        }

        public async Task<IActionResult> OAuthCallback(string code, string state)
        {
            if (!string.IsNullOrEmpty(code))
            {
                var accessToken = await _calendarService.HandleCallbackAsync(code, state);
                HttpContext.Session.SetString($"GraphToken_{state}", accessToken);
                TempData["Success"] = "Connected to Microsoft Outlook successfully!";
            }
            
            return RedirectToAction(nameof(MyAppointments));
        }
    }
}
