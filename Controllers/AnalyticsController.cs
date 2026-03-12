using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin,Admin,Sales Manager,Marketing Manager,Billing Staff")]
    public class AnalyticsController : Controller
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public AnalyticsController(Data.ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string filter = "30Days")
        {
            ViewData["CurrentPage"] = "Analytics";

            // Determine the timeframe based on the filter
            DateTime startDate;
            switch (filter)
            {
                case "7Days":
                    startDate = DateTime.UtcNow.AddDays(-7);
                    break;
                case "Year":
                    startDate = new DateTime(DateTime.UtcNow.Year, 1, 1);
                    break;
                case "30Days":
                default:
                    startDate = DateTime.UtcNow.AddDays(-30);
                    break;
            }

            ViewBag.CurrentFilter = filter;

            var totalRevenue = await _context.Orders
                .Where(o => o.Status == Models.OrderStatus.Completed && o.OrderDate >= startDate)
                .SumAsync(o => o.TotalAmount);
                
            var activeUsers = _userManager.Users.Count(); // Active users usually isn't filtered
            var totalLeads = await _context.Leads.CountAsync(l => l.CreatedAt >= startDate);
            var convertedLeads = await _context.Leads.CountAsync(l => l.Status == "Converted" && l.CreatedAt >= startDate);
            var conversionRate = totalLeads > 0 ? (double)convertedLeads / totalLeads * 100 : 0;
            
            var ticketDates = await _context.SupportTickets
                .Where(t => t.LastUpdated.HasValue && t.CreatedAt >= startDate)
                .Select(t => new { t.LastUpdated, t.CreatedAt })
                .ToListAsync();

            var avgResponseTime = ticketDates.Any() 
                ? ticketDates.Average(t => (t.LastUpdated.Value - t.CreatedAt).TotalHours)
                : 0;

            var viewModel = new ViewModels.AnalyticsViewModel
            {
                TotalRevenue = totalRevenue,
                ActiveUsers = activeUsers,
                ConversionRate = conversionRate,
                AvgResponseTimeHours = avgResponseTime,
                RevenueGrowth = 15.3, // Placeholder
                UserGrowth = 8.2,     // Placeholder
                ConversionGrowth = 3.1, // Placeholder
                ResponseTimeImprovement = -12.4, // Placeholder
                TopPerformingModule = "Customer Master Data",
                TopModuleUsers = _context.Customers.Count(),
                BestConversionModule = "Sales Management",
                BestConversionRate = conversionRate,
                FastestResponseModule = "Customer Support",
                FastestResponseTime = avgResponseTime
            };

            // Weekly ticket trends (last 7 days)
            var today = DateTime.UtcNow.Date;
            var daysOfWeek = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            
            for (int i = 0; i < 7; i++)
            {
                var targetDate = today.AddDays(-6 + i);
                var dayName = daysOfWeek[i];
                
                var ticketsOnDay = await _context.SupportTickets
                    .Where(t => t.CreatedAt.Date == targetDate)
                    .ToListAsync();
                
                viewModel.WeeklyTicketTrends[dayName] = new ViewModels.TicketTrendData
                {
                    Closed = ticketsOnDay.Count(t => t.Status == "Resolved" || t.Status == "Closed"),
                    Opened = ticketsOnDay.Count(t => t.Status == "Open" || t.Status == "New"),
                    Pending = ticketsOnDay.Count(t => t.Status == "In Progress" || t.Status == "Pending")
                };
            }

            // Monthly lead trends (last 6 months)
            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            for (int i = 0; i < 6; i++)
            {
                var targetMonth = DateTime.UtcNow.AddMonths(-5 + i);
                var monthName = monthNames[i];
                
                var leadsInMonth = await _context.Leads
                    .Where(l => l.CreatedAt.Year == targetMonth.Year && l.CreatedAt.Month == targetMonth.Month)
                    .CountAsync();
                
                viewModel.MonthlyLeadTrends[monthName] = leadsInMonth;
            }

            return View(viewModel);
        }
    }
}
