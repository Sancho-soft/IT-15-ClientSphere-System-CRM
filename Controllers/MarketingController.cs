using ClientSphere.ViewModels;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Threading.Tasks;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Marketing Manager,Admin")]
    public class MarketingController : Controller
    {
        private readonly ICampaignService _campaignService;
        private readonly ICloudinaryService _cloudinaryService;

        public MarketingController(ICampaignService campaignService, ICloudinaryService cloudinaryService)
        {
            _campaignService = campaignService;
            _cloudinaryService = cloudinaryService;
        }

        public async Task<IActionResult> Index(bool archived = false)
        {
            var allCampaigns = await _campaignService.GetAllCampaignsAsync();
            var campaigns = archived
                ? allCampaigns.Where(c => c.IsArchived).ToList()
                : allCampaigns.Where(c => !c.IsArchived).ToList();
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
                    ManagedBy = "Marketing Team",
                    ImageUrl = c.ImageUrl
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
        public async Task<IActionResult> Create(Models.Campaign campaign, IFormFile? campaignBanner)
        {
            if (ModelState.IsValid)
            {
                if (campaignBanner != null && campaignBanner.Length > 0)
                {
                    try
                    {
                        string? url = await _cloudinaryService.UploadImageAsync(campaignBanner, "campaigns");
                        if (!string.IsNullOrEmpty(url)) campaign.ImageUrl = url;
                    }
                    catch (Exception ex)
                    {
                        ModelState.AddModelError("", "Failed to upload banner: " + ex.Message);
                        return View(campaign);
                    }
                }

                campaign.Status = "Planned";
                campaign.StartDate = DateTime.UtcNow;
                if (!campaign.EndDate.HasValue) campaign.EndDate = campaign.StartDate.AddDays(30);
                await _campaignService.CreateCampaignAsync(campaign);
                return RedirectToAction(nameof(Index));
            }
            return View(campaign);
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null) return NotFound();
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Models.Campaign campaign, IFormFile? campaignBanner)
        {
            if (id != campaign.Id) return NotFound();
            if (ModelState.IsValid)
            {
                try 
                { 
                    if (campaignBanner != null && campaignBanner.Length > 0)
                    {
                        string? url = await _cloudinaryService.UploadImageAsync(campaignBanner, "campaigns");
                        if (!string.IsNullOrEmpty(url)) campaign.ImageUrl = url;
                    }
                    else
                    {
                        // Preserve existing image if not uploading a new one
                        var existingCampaign = await _campaignService.GetCampaignByIdAsync(id);
                        if (existingCampaign != null) campaign.ImageUrl = existingCampaign.ImageUrl;
                    }

                    await _campaignService.UpdateCampaignAsync(campaign); 
                    return RedirectToAction(nameof(Index)); 
                }
                catch (Exception) { if (await _campaignService.GetCampaignByIdAsync(id) == null) return NotFound(); throw; }
            }
            return View(campaign);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ArchiveCampaign(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null) return NotFound();
            campaign.IsArchived = true;
            campaign.ArchivedAt = DateTime.UtcNow;
            await _campaignService.UpdateCampaignAsync(campaign);
            TempData["Success"] = $"\"{campaign.Name}\" has been archived.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnarchiveCampaign(int id)
        {
            var campaign = await _campaignService.GetCampaignByIdAsync(id);
            if (campaign == null) return NotFound();
            campaign.IsArchived = false;
            campaign.ArchivedAt = null;
            await _campaignService.UpdateCampaignAsync(campaign);
            TempData["Success"] = $"\"{campaign.Name}\" has been restored.";
            return RedirectToAction(nameof(Index), new { archived = true });
        }
    }
}
