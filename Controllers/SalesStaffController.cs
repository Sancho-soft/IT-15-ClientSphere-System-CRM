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
