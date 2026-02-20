using System.Collections.Generic;
using System.Threading.Tasks;
using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Services
{
    public class CampaignService : ICampaignService
    {
        private readonly ApplicationDbContext _context;

        public CampaignService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Campaign>> GetAllCampaignsAsync()
        {
            return await _context.Campaigns.OrderByDescending(c => c.StartDate).ToListAsync();
        }

        public async Task<List<Campaign>> GetCampaignsByManagerAsync(string userId)
        {
            // In a real scenario, filter by ManagedByUserId if applicable
            // For now, return all campaigns but you could filter: .Where(c => c.ManagedByUserId == userId)
            return await _context.Campaigns.OrderByDescending(c => c.StartDate).ToListAsync();
        }

        public async Task<Campaign?> GetCampaignByIdAsync(int id)
        {
            return await _context.Campaigns.FindAsync(id);
        }

        public async Task CreateCampaignAsync(Campaign campaign)
        {
            _context.Campaigns.Add(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateCampaignAsync(Campaign campaign)
        {
            _context.Campaigns.Update(campaign);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteCampaignAsync(int id)
        {
            var campaign = await _context.Campaigns.FindAsync(id);
            if (campaign != null)
            {
                _context.Campaigns.Remove(campaign);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<Dictionary<string, int>> GetCampaignStatsAsync()
        {
            var stats = new Dictionary<string, int>
            {
                { "Total", await _context.Campaigns.CountAsync() },
                { "Active", await _context.Campaigns.CountAsync(c => c.Status == "Active") },
                { "Draft", await _context.Campaigns.CountAsync(c => c.Status == "Draft") },
                { "Completed", await _context.Campaigns.CountAsync(c => c.Status == "Completed") }
            };
            return stats;
        }
    }
}
