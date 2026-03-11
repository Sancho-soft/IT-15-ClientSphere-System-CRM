using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;

        public NotificationService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateForUserAsync(string userId, string title, string message, string? url = null, string? category = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                Url = url,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task CreateForRoleAsync(string role, string title, string message, string? url = null, string? category = null)
        {
            // Use special virtual UserId "__ROLE__:RoleName" to indicate role-based notification
            _context.Notifications.Add(new Notification
            {
                UserId = $"__ROLE__{role}",
                ForRole = role,
                Title = title,
                Message = message,
                Url = url,
                Category = category,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetUnreadForUserAsync(string userId, IList<string> userRoles)
        {
            var roleIds = userRoles.Select(r => $"__ROLE__{r}").ToList();
            return await _context.Notifications
                .Where(n => !n.IsRead && (n.UserId == userId || roleIds.Contains(n.UserId)))
                .OrderByDescending(n => n.CreatedAt)
                .Take(20)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountForUserAsync(string userId, IList<string> userRoles)
        {
            var roleIds = userRoles.Select(r => $"__ROLE__{r}").ToList();
            return await _context.Notifications
                .CountAsync(n => !n.IsRead && (n.UserId == userId || roleIds.Contains(n.UserId)));
        }

        public async Task MarkReadAsync(int id, string userId)
        {
            var notification = await _context.Notifications.FindAsync(id);
            if (notification != null)
            {
                notification.IsRead = true;
                await _context.SaveChangesAsync();
            }
        }

        public async Task MarkAllReadForUserAsync(string userId, IList<string> userRoles)
        {
            var roleIds = userRoles.Select(r => $"__ROLE__{r}").ToList();
            var unread = await _context.Notifications
                .Where(n => !n.IsRead && (n.UserId == userId || roleIds.Contains(n.UserId)))
                .ToListAsync();

            foreach (var n in unread) n.IsRead = true;
            await _context.SaveChangesAsync();
        }
    }
}
