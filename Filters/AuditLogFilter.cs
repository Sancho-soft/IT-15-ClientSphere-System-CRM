using Microsoft.AspNetCore.Mvc.Filters;
using System.Threading.Tasks;
using ClientSphere.Data;
using ClientSphere.Constants;
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

            // Only log successful, non-GET actions by authenticated users (Finding #10)
            if (resultContext.Exception != null || resultContext.Canceled)
                return;

            if (context.HttpContext.Request.Method != "GET" && context.HttpContext.User.Identity?.IsAuthenticated == true)
            {
                var action = context.RouteData.Values["action"]?.ToString();
                var controller = context.RouteData.Values["controller"]?.ToString();

                var user = await _userManager.GetUserAsync(context.HttpContext.User);
                string roleName = StatusValues.Unknown;
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    roleName = roles.Count > 0 ? roles[0] : StatusValues.NoRole; // Finding #17: use [0] instead of .First()
                }

                var log = new AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = user?.Id ?? StatusValues.Unknown,
                    UserName = $"{user?.FirstName ?? context.HttpContext.User.Identity.Name} ({roleName})",
                    Action = $"{context.HttpContext.Request.Method} {controller}/{action}",
                    Description = $"Role [{roleName}] executed {action} in {controller}",
                    IpAddress = context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? StatusValues.Unknown
                };

                _context.AuditLogs.Add(log);
                await _context.SaveChangesAsync();
            }
        }
    }
}
