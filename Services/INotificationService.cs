using System.Threading.Tasks;
using System.Collections.Generic;
using ClientSphere.Models;

namespace ClientSphere.Services
{
    public interface INotificationService
    {
        Task CreateForUserAsync(string userId, string title, string message, string? url = null, string? category = null);
        Task CreateForRoleAsync(string role, string title, string message, string? url = null, string? category = null);
        Task<List<Notification>> GetUnreadForUserAsync(string userId, IList<string> userRoles);
        Task<int> GetUnreadCountForUserAsync(string userId, IList<string> userRoles);
        Task MarkReadAsync(int id, string userId);
        Task MarkAllReadForUserAsync(string userId, IList<string> userRoles);
    }
}
