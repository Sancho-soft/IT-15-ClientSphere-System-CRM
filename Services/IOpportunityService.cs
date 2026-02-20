using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface IOpportunityService
    {
        Task<IEnumerable<Opportunity>> GetOpportunitiesBySalesStaffAsync(string userId);
        Task<Opportunity?> GetOpportunityByIdAsync(int id);
        Task AddOpportunityAsync(Opportunity opportunity);
        Task UpdateOpportunityAsync(Opportunity opportunity);
    }
}
