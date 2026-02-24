using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using ClientSphere.Data;
using ClientSphere.Models;
using Microsoft.AspNetCore.Identity;
using System;
using System.Linq;

namespace ClientSphere.Filters
{
    public class AuditLogFilter : IAsyncActionFilter
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AuditLogFilter(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var resultContext = await next();

            if (context.HttpContext.Request.Method != "GET" && context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var action = context.RouteData.Values["action"]?.ToString();
                var controller = context.RouteData.Values["controller"]?.ToString();

                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                string roleName = "Unknown Role";
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    roleName = roles.Any() ? roles.First() : "No Role";
                }

                var log = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = user?.Id ?? "Unknown",
                    UserName = $"{user?.FirstName ?? context.HttpContext.User.Identity.Name} ({roleName})",
                    Action = $"{context.HttpContext.Request.Method} {controller}/{action}",
                    Description = $"Role [{roleName}] executed {action} in {controller}",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}
