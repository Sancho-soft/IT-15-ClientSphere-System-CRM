using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface ISupportService
    {
        Task<IEnumerable<SupportTicket>> GetAllTicketsAsync();
        Task<IEnumerable<SupportTicket>> GetTicketsByCustomerIdAsync(string customerId);
        Task<SupportTicket?> GetTicketByIdAsync(int id);
        Task CreateTicketAsync(SupportTicket ticket);
        Task UpdateTicketAsync(SupportTicket ticket);
        Task UpdateTicketStatusAsync(int ticketId, string status);
        Task DeleteTicketAsync(int ticketId);
    }
}
