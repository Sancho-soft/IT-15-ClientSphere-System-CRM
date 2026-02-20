using ClientSphere.ViewModels;
using ClientSphere.Services;
using ClientSphere.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Marketing Staff, Admin")]
    public class MarketingStaffController : Controller
    {
        private readonly ICampaignService _campaignService;

        public MarketingStaffController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var campaigns = await _campaignService.GetAllCampaignsAsync();
            var stats = await _campaignService.GetCampaignStatsAsync(); // Assuming this method returns a Dictionary

            // Calculate totals
            var totalRecipients = campaigns.Sum(c => c.ActualRecipients);
            // Avoid division by zero
            var totalSent = campaigns.Sum(c => c.ActualRecipients);
            var totalResponses = campaigns.Sum(c => c.Clicks + c.Conversions); // Simplified response metric
            double avgResponseRate = totalSent > 0 ? (double)totalResponses / totalSent * 100 : 0;

            var viewModel = new MarketingStaffDashboardViewModel
            {
                TotalCampaigns = campaigns.Count,
                TotalRecipients = totalRecipients,
                AvgResponseRate = Math.Round(avgResponseRate, 1),
                Campaigns = campaigns.Select(c => new CampaignStaffViewModel
                {
                    CampaignId = $"CAMP-{c.Id}", // Formatting ID
                    Name = c.Name,
                    Type = c.Type,
                    Status = c.Status,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate ?? DateTime.MinValue,
                    Recipients = c.ActualRecipients,
                    Responses = c.Clicks + c.Conversions,
                    ResponseRate = c.ActualRecipients > 0 ? Math.Round((double)(c.Clicks + c.Conversions) / c.ActualRecipients * 100, 1) : 0,
                    ManagedBy = "You" // Simplified, or fetch user name via UserManager
                }).ToList()
            };

            return View(viewModel);
        }

        // GET: Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Campaign campaign)
        {
            if (ModelState.IsValid)
            {
                // Set default values
                campaign.Status = "Planned"; 
                // campaign.ManagedBy = User.Identity.Name; // If we had UserId field

                await _campaignService.CreateCampaignAsync(campaign);
                return RedirectToAction(nameof(Dashboard));
            }
            return View(campaign);
        }
    }
}
