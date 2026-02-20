using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface ICampaignService
    {
        Task<List<Campaign>> GetAllCampaignsAsync();
        Task<List<Campaign>> GetCampaignsByManagerAsync(string userId);
        Task<Campaign?> GetCampaignByIdAsync(int id);
        Task CreateCampaignAsync(Campaign campaign);
        Task UpdateCampaignAsync(Campaign campaign);
        Task DeleteCampaignAsync(int id);
        Task<Dictionary<string, int>> GetCampaignStatsAsync();
    }
}
