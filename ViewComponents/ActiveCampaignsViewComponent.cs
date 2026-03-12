using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using ClientSphere.Services;

namespace ClientSphere.ViewComponents
{
    public class ActiveCampaignsViewComponent : ViewComponent
    {
        private readonly ICampaignService _campaignService;

        public ActiveCampaignsViewComponent(ICampaignService campaignService)
        {
            _campaignService = campaignService;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var allCampaigns = await _campaignService.GetAllCampaignsAsync();
            var activeCampaigns = allCampaigns
                .Where(c => c.Status == "Active" && !c.IsArchived)
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .ToList();

            return View(activeCampaigns);
        }
    }
}
