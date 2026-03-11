using ClientSphere.Models;
using ClientSphere.Services;
using ClientSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Sales Manager,Admin")]
    public class SalesManagerController : Controller
    {
        private readonly IOrderService _orderService;
        private readonly ILeadService _leadService;
        private readonly IOpportunityService _opportunityService;
        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public SalesManagerController(
            IOrderService orderService,
            ILeadService leadService,
            IOpportunityService opportunityService,
            Data.ApplicationDbContext context,
            Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _orderService = orderService;
            _leadService = leadService;
            _opportunityService = opportunityService;
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            var stats = await _orderService.GetSalesManagerStatsAsync();

            // Calculate Team Performance from Closed Won Opportunities
            // In a real app, we'd likely have a dedicated Sales Performance service/table, 
            // but aggregating Closed Opportunities is a valid proxy for "Sales made by Staff".
            var teamPerformance = new List<SalesPersonPerformance>();

            try 
            {
                var salesStaffRole = await _userManager.GetUsersInRoleAsync("Sales Staff");
                
                // Fetch all closed won opportunities first to ensure grouping works in memory
                var wonOpps = await _context.Opportunities
                    .Where(o => o.Stage == "Closed Won")
                    .ToListAsync();

                var salesData = wonOpps
                    .GroupBy(o => o.AssignedToUserId)
                    .Select(g => new 
                    { 
                        UserId = g.Key, 
                        TotalSales = g.Sum(x => x.EstimatedValue), 
                        DealsCount = g.Count() 
                    })
                    .ToList();

                int rank = 1;
                foreach (var user in salesStaffRole)
                {
                    var data = salesData.FirstOrDefault(d => d.UserId == user.Id);
                    teamPerformance.Add(new SalesPersonPerformance
                    {
                        Rank = 0, // Will sort and assign later
                        Name = $"{user.FirstName} {user.LastName}",
                        Role = "Sales Staff",
                        TotalSales = data?.TotalSales ?? 0,
                        DealsCount = data?.DealsCount ?? 0,
                        Growth = 0 // Growth calculation requires historical data which we lack for Opportunities, defaulting to 0
                    });
                }
                
                // Sort by Sales and assign Rank
                teamPerformance = teamPerformance.OrderByDescending(p => p.TotalSales).ToList();
                for(int i = 0; i < teamPerformance.Count; i++)
                {
                    teamPerformance[i].Rank = i + 1;
                }
            }
            catch (Exception ex)
            {
                // Fallback or log if something fails (e.g. role doesn't exist)
                Console.WriteLine($"Error calculating team performance: {ex.Message}");
            }

            var viewModel = new SalesManagerDashboardViewModel
            {
                TotalRevenueMTD = stats.TotalRevenueMTD,
                RevenueGrowth = stats.RevenueGrowth,
                QuotaAchievement = stats.QuotaAchievement,
                DealsClosedMTD = stats.DealsClosedMTD,
                DealsGrowth = stats.DealsGrowth,
                AvgDealSize = stats.AvgDealSize,

                TeamPerformance = teamPerformance
            };

            return View(viewModel);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveOrder(int id)
        {
            await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Processing);
            return RedirectToAction(nameof(Dashboard));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectOrder(int id)
        {
            await _orderService.UpdateOrderStatusAsync(id, OrderStatus.Cancelled);
            return RedirectToAction(nameof(Dashboard));
        }

        // --- TEAM-WIDE VIEWS ---

        public async Task<IActionResult> AllLeads()
        {
            var allLeads = await _leadService.GetAllLeadsAsync();
            return View(allLeads);
        }

        public async Task<IActionResult> AllOpportunities()
        {
            var allOpportunities = await _opportunityService.GetAllOpportunitiesAsync();
            return View(allOpportunities);
        }

        public async Task<IActionResult> AllAppointments()
        {
            var allAppointments = await _context.Appointments
                .Include(a => a.Customer)
                .OrderBy(a => a.StartTime)
                .ToListAsync();
            return View(allAppointments);
        }

        // --- CREATE APPOINTMENT ---

        [HttpGet]
        public async Task<IActionResult> CreateAppointment()
        {
            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();

            var customers = await _context.Customers.ToListAsync();
            ViewBag.CustomerList = customers
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ContactName
                }).ToList();

            return View(new Appointment { StartTime = DateTime.UtcNow, EndTime = DateTime.UtcNow.AddHours(1) });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateAppointment([Bind("Title,Description,StartTime,EndTime,Location,Status,OrganizerUserId,CustomerId")] Appointment appointment)
        {
            if (ModelState.IsValid)
            {
                _context.Appointments.Add(appointment);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Appointment scheduled and assigned to Sales Staff successfully!";
                return RedirectToAction(nameof(AllAppointments));
            }

            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}"
                }).ToList();

            var customers = await _context.Customers.ToListAsync();
            ViewBag.CustomerList = customers
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ContactName
                }).ToList();

            return View(appointment);
        }

        // --- EDIT & REASSIGN ---

        [HttpGet]
        public async Task<IActionResult> EditLead(int? id)
        {
            if (id == null) return NotFound();
            var lead = await _leadService.GetLeadByIdAsync(id.Value);
            if (lead == null) return NotFound();

            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}",
                    Selected = u.Id == lead.AssignedToUserId
                }).ToList();

            return View(lead);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLead(int id, [Bind("Id,FirstName,LastName,Email,Phone,Company,Source,Status,CreatedAt,AssignedToUserId")] Models.Lead lead)
        {
            if (id != lead.Id) return NotFound();
            if (ModelState.IsValid)
            {
                await _leadService.UpdateLeadAsync(lead);
                TempData["Success"] = "Lead updated and reassigned successfully!";
                return RedirectToAction(nameof(AllLeads));
            }

            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}",
                    Selected = u.Id == lead.AssignedToUserId
                }).ToList();
            return View(lead);
        }

        [HttpGet]
        public async Task<IActionResult> EditOpportunity(int? id)
        {
            if (id == null) return NotFound();
            var opp = await _opportunityService.GetOpportunityByIdAsync(id.Value);
            if (opp == null) return NotFound();

            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}",
                    Selected = u.Id == opp.AssignedToUserId
                }).ToList();

            return View(opp);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditOpportunity(int id, [Bind("Id,Name,EstimatedValue,Stage,Probability,ExpectedCloseDate,CreatedAt,AssignedToUserId")] Models.Opportunity opp)
        {
            if (id != opp.Id) return NotFound();
            if (ModelState.IsValid)
            {
                await _opportunityService.UpdateOpportunityAsync(opp);
                TempData["Success"] = "Opportunity updated and reassigned successfully!";
                return RedirectToAction(nameof(AllOpportunities));
            }

            var salesStaff = await _userManager.GetUsersInRoleAsync("Sales Staff");
            ViewBag.SalesStaffList = salesStaff
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.Id,
                    Text = $"{u.FirstName} {u.LastName}",
                    Selected = u.Id == opp.AssignedToUserId
                }).ToList();
            return View(opp);
        }
    }
}
