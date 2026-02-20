using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface ILeadService
    {
        Task<IEnumerable<Lead>> GetLeadsBySalesStaffAsync(string userId);
        Task<Lead?> GetLeadByIdAsync(int id);
        Task AddLeadAsync(Lead lead);
        Task UpdateLeadAsync(Lead lead);
    }
}
