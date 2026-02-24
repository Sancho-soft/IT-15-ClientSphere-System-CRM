using ClientSphere.ViewModels;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class MarketingController : Controller
    {
        private readonly ICampaignService _campaignService;

        public MarketingController(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        public async Task<IActionResult> Index(bool archived = false)
        {
            var allCampaigns = await _campaignService.GetAllCampaignsAsync();
            var campaigns = archived ? allCampaigns.Where(c => c.Status == "Completed" || c.Status == "Cancelled") : allCampaigns.Where(c => c.Status != "Completed" && c.Status != "Cancelled");
            // Also need to fix the sum logic to not break if list is empty, but we convert to List first
            campaigns = campaigns.ToList();
            ViewData["IsArchived"] = archived;
            
            var viewModel = new MarketingDashboardViewModel
            {
                TotalCampaigns = campaigns.Count(),
                ActiveBudget = campaigns.Any(c => c.Status == "Active") ? campaigns.Where(c => c.Status == "Active").Sum(c => c.Budget) : 0,
                TotalRecipients = campaigns.Any() ? campaigns.Sum(c => c.TargetAudienceSize) : 0,
                AvgResponseRate = campaigns.Any() ? campaigns.Average(c => ((double)c.ExpectedRevenue / (double)c.Budget) * 100) : 0,
                Campaigns = campaigns.Select(c => new CampaignViewModel
                {
                    Id = c.Id,
                    CampaignId = $"CAMP-{c.Id:D3}",
                    Name = c.Name,
                    Type = c.Type,
                    Status = c.Status,
                    Budget = c.Budget,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate ?? DateTime.UtcNow.AddDays(30),
                    Recipients = c.TargetAudienceSize,
                    Responses = (int)(c.ExpectedRevenue / 100),
                    ResponseRate = ((double)c.ExpectedRevenue / (double)c.Budget) * 100,
                    ManagedBy = "Marketing Team"
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
        public async Task<IActionResult> Create(Models.Campaign campaign)
        {
            if (ModelState.IsValid)
            {
                campaign.Status = "Planned";
                campaign.StartDate = DateTime.UtcNow;
                // Ensure EndDate is after StartDate if not provided
                if (!campaign.EndDate.HasValue)
                {
                    campaign.EndDate = campaign.StartDate.AddDays(30);
                }

                await _campaignService.CreateCampaignAsync(campaign);
                return RedirectToAction(nameof(Index));
            }
            return View(campaign);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Campaign campaign)
        {
            if (id != campaign.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    await _campaignService.UpdateCampaignAsync(campaign);
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception)
                {
                    if (await _campaignService.GetCampaignByIdAsync(id) == null)
                    {
                        return NotFound();
                    }
                    throw;
                }
            }
            return View(campaign);
        }
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null)
            {
                return NotFound();
            }
            return View(campaign);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _campaignService.DeleteCampaignAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
