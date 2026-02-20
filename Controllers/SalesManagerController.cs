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
        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public SalesManagerController(IOrderService orderService, Data.ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _orderService = orderService;
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
                
                TeamPerformance = teamPerformance,

                PendingHighValueDeals = stats.PendingHighValueDeals
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
    }
}
