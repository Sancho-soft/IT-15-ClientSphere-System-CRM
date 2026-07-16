using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ClientSphere.Models;
using ClientSphere.Constants;
using System.Threading.Tasks;
using System;

namespace ClientSphere.Filters
{
    public class EnforceMfaFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public EnforceMfaFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(httpContext.User);
                if (user != null)
                {
                    // Check if they are Super Admin or Admin
                    var roles = await _userManager.GetRolesAsync(user);
                    if (roles.Contains(Roles.SuperAdmin) || roles.Contains(Roles.Admin))
                    {
                        var is2faEnabled = await _userManager.GetTwoFactorEnabledAsync(user);
                        if (!is2faEnabled)
                        {
                            var path = httpContext.Request.Path.Value ?? "";
                            
                            // Prevent infinite redirection loops
                            var isManagePage = path.Contains("/Identity/Account/Manage", StringComparison.OrdinalIgnoreCase);
                            var isLogoutPage = path.Contains("/Identity/Account/Logout", StringComparison.OrdinalIgnoreCase);

                            if (!isManagePage && !isLogoutPage)
                            {
                                // Redirect to Two Factor Authentication setup
                                context.Result = new RedirectToPageResult("/Account/Manage/TwoFactorAuthentication", new { area = "Identity" });
                                return;
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
