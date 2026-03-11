using ClientSphere.Data;
using ClientSphere.Models;
using ClientSphere.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize]
    public class NotificationsController : Controller
    {
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;

        public NotificationsController(INotificationService notificationService, UserManager<ApplicationUser> userManager)
        {
            _notificationService = notificationService;
            _userManager = userManager;
        }

        // GET: /Notifications/GetUnread — returns JSON for bell dropdown
        [HttpGet]
        public async Task<IActionResult> GetUnread()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var roles = await _userManager.GetRolesAsync(user);
            var notifications = await _notificationService.GetUnreadForUserAsync(user.Id, roles);
            var count = notifications.Count;

            return Json(new
            {
                count,
                items = notifications.Select(n => new
                {
                    n.Id,
                    n.Title,
                    n.Message,
                    n.Url,
                    n.Category,
                    n.CreatedAt,
                    TimeAgo = GetTimeAgo(n.CreatedAt)
                })
            });
        }

        // POST: /Notifications/MarkRead/5
        [HttpPost]
        public async Task<IActionResult> MarkRead(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            await _notificationService.MarkReadAsync(id, user.Id);
            return Ok();
        }

        // POST: /Notifications/MarkAllRead
        [HttpPost]
        public async Task<IActionResult> MarkAllRead()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var roles = await _userManager.GetRolesAsync(user);
            await _notificationService.MarkAllReadForUserAsync(user.Id, roles);
            return Ok();
        }

        private static string GetTimeAgo(DateTime utcTime)
        {
            var diff = DateTime.UtcNow - utcTime;
            if (diff.TotalMinutes < 1) return "Just now";
            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
            return $"{(int)diff.TotalDays}d ago";
        }
    }
}
