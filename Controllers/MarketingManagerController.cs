using ClientSphere.Services;
using ClientSphere.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Marketing Manager, Admin, Super Admin")]
    public class MarketingManagerController : Controller
    {
        private readonly ICampaignService _campaignService;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public MarketingManagerController(
            ICampaignService campaignService,
            Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _campaignService = campaignService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["CurrentPage"] = "Dashboard";
            ViewData["HideTopNavTitle"] = true;

            var campaigns = await _campaignService.GetAllCampaignsAsync();

            // Team members in Marketing Staff role
            var marketingStaff = await _userManager.GetUsersInRoleAsync("Marketing Staff");

            // Aggregate stats
            var totalCampaigns    = campaigns.Count;
            var activeCampaigns   = campaigns.Count(c => c.Status == "Active");
            var completedCampaigns = campaigns.Count(c => c.Status == "Completed");
            var totalRecipients   = campaigns.Sum(c => c.ActualRecipients);
            var totalResponses    = campaigns.Sum(c => c.Clicks + c.Conversions);
            double avgResponseRate = totalRecipients > 0
                ? Math.Round((double)totalResponses / totalRecipients * 100, 1)
                : 0;
            var totalBudget       = campaigns.Sum(c => c.Budget);
            var totalConversions  = campaigns.Sum(c => c.Conversions);

            // Team performance — group campaigns by type for overview
            var campaignsByType = campaigns
                .GroupBy(c => c.Type ?? "Other")
                .Select(g => new CampaignTypeStats
                {
                    Type        = g.Key,
                    Count       = g.Count(),
                    TotalBudget = g.Sum(x => x.Budget),
                    Conversions = g.Sum(x => x.Conversions)
                })
                .OrderByDescending(x => x.Count)
                .ToList();

            var recentCampaigns = campaigns
                .OrderByDescending(c => c.StartDate)
                .Take(10)
                .Select(c => new CampaignStaffViewModel
                {
                    CampaignId   = $"CAMP-{c.Id}",
                    Name         = c.Name,
                    Type         = c.Type,
                    Status       = c.Status,
                    StartDate    = c.StartDate,
                    EndDate      = c.EndDate ?? DateTime.MinValue,
                    Recipients   = c.ActualRecipients,
                    Responses    = c.Clicks + c.Conversions,
                    ResponseRate = c.ActualRecipients > 0
                        ? Math.Round((double)(c.Clicks + c.Conversions) / c.ActualRecipients * 100, 1)
                        : 0,
                    ManagedBy = "Marketing Team"
                })
                .ToList();

            var viewModel = new MarketingManagerDashboardViewModel
            {
                TotalCampaigns     = totalCampaigns,
                ActiveCampaigns    = activeCampaigns,
                CompletedCampaigns = completedCampaigns,
                TotalRecipients    = totalRecipients,
                TotalConversions   = totalConversions,
                AvgResponseRate    = avgResponseRate,
                TotalBudget        = totalBudget,
                TeamMemberCount    = marketingStaff.Count,
                CampaignsByType    = campaignsByType,
                RecentCampaigns    = recentCampaigns
            };

            return View(viewModel);
        }
    }
}
