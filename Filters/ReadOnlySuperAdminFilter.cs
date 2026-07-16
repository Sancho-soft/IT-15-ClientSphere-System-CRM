using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using ClientSphere.Models;
using ClientSphere.Constants;
using System.Threading.Tasks;
using System;
using System.Linq;

namespace ClientSphere.Filters
{
    public class ReadOnlySuperAdminFilter : IAsyncActionFilter
    {
        private readonly UserManager<ApplicationUser> _userManager;

        public ReadOnlySuperAdminFilter(UserManager<ApplicationUser> userManager)
        {
            _userManager = userManager;
        }

        public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var request = httpContext.Request;

            // Check if user is logged in
            if (httpContext.User.Identity?.IsAuthenticated == true)
            {
                var user = await _userManager.GetUserAsync(httpContext.User);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    
                    // If user is a Super Admin
                    if (roles.Contains(Roles.SuperAdmin))
                    {
                        var method = request.Method.ToUpperInvariant();
                        
                        // Check for write/mutation methods (POST, PUT, DELETE)
                        if (method == "POST" || method == "PUT" || method == "DELETE")
                        {
                            var areaName = context.RouteData.Values["area"]?.ToString();
                            var controllerName = context.RouteData.Values["controller"]?.ToString();
                            
                            // Define allowed controllers where Super Admin has full write access (Admin dashboard, Profile, etc.)
                            var allowedControllers = new[] { "Admin", "Profile", "Home", "Notifications", "PaymongoWebhook" };

                            bool isAllowed = false;
                            if (string.Equals(areaName, "Identity", StringComparison.OrdinalIgnoreCase))
                            {
                                isAllowed = true;
                            }
                            else if (controllerName != null && allowedControllers.Contains(controllerName, StringComparer.OrdinalIgnoreCase))
                            {
                                isAllowed = true;
                            }

                            if (!isAllowed)
                            {
                                // Block write operations in CRM modules for Super Admin
                                var isAjax = request.Headers["X-Requested-With"] == "XMLHttpRequest" ||
                                             request.Headers["Accept"].ToString().Contains("application/json");

                                if (isAjax)
                                {
                                    context.Result = new JsonResult(new { 
                                        success = false, 
                                        message = "Access Denied: Super Admin is restricted to view-only mode in CRM modules." 
                                    }) { StatusCode = 403 };
                                    return;
                                }
                                else
                                {
                                    var controller = context.Controller as Controller;
                                    if (controller != null)
                                    {
                                        controller.TempData["ErrorMessage"] = "Access Denied: Super Admin is restricted to view-only mode in CRM modules.";
                                    }

                                    var referer = request.Headers["Referer"].ToString();
                                    if (!string.IsNullOrEmpty(referer))
                                    {
                                        context.Result = new RedirectResult(referer);
                                    }
                                    else
                                    {
                                        context.Result = new RedirectToActionResult("Index", controllerName, null);
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }

            await next();
        }
    }
}
