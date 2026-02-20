using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Services
{
    public class OpportunityService : IOpportunityService
    {
        private readonly ApplicationDbContext _context;

        public OpportunityService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Opportunity>> GetOpportunitiesBySalesStaffAsync(string userId)
        {
            return await _context.Opportunities
                .Where(o => o.AssignedToUserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Opportunity?> GetOpportunityByIdAsync(int id)
        {
            return await _context.Opportunities.FindAsync(id);
        }

        public async Task AddOpportunityAsync(Opportunity opportunity)
        {
            _context.Opportunities.Add(opportunity);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateOpportunityAsync(Opportunity opportunity)
        {
            _context.Opportunities.Update(opportunity);
            await _context.SaveChangesAsync();
        }
    }
}
