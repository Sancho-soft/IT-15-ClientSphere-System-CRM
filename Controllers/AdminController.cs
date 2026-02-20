using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin, Admin")]
    public class AdminController : Controller
    {
        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;

        public AdminController(Data.ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData["CurrentPage"] = "Dashboard";

            var recentActivities = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToListAsync();

            var viewModel = new ViewModels.AdminDashboardViewModel
            {
                TotalUsers = _userManager.Users.Count(),
                TotalOrders = _context.Orders.Count(),
                TotalRevenue = _context.Orders.Where(o => o.Status == Models.OrderStatus.Completed).Sum(o => o.TotalAmount),
                ActiveTickets = _context.SupportTickets.Count(t => t.Status != "Resolved" && t.Status != "Closed"),
                PendingLeads = _context.Leads.Count(l => l.Status == "New" || l.Status == "Contacted"),
                ActiveCampaigns = _context.Campaigns.Count(c => c.Status == "Active"),
                PendingInvoices = _context.Invoices.Count(i => i.Status == "Pending" || i.Status == "Sent"),
                RecentActivities = recentActivities
            };

            return View(viewModel);
        }


        // User Management - Super Admin Only
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> UserManagement()
        {
            ViewData["CurrentPage"] = "User Management";

            var users = _userManager.Users.ToList();
            var userViewModels = new List<ViewModels.UserItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "No Role";
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = user.UserName ?? "Unknown";

                userViewModels.Add(new ViewModels.UserItemViewModel
                {
                    Id = user.Id,
                    FullName = fullName,
                    Email = user.Email ?? "No Email",
                    Initials = GetInitials(fullName),
                    Role = roleName,
                    Status = user.IsActive ? "Active" : "Inactive",
                    LastActive = user.LastLoginDate ?? user.CreatedAt,
                    LastActiveDisplay = GetLastActiveDisplay(user.LastLoginDate ?? user.CreatedAt)
                });
            }

            var viewModel = new ViewModels.UserManagementViewModel
            {
                Users = userViewModels,
                TotalUsers = users.Count,
                AdminCount = userViewModels.Count(u => u.Role == "Admin"),
                SalesTeamCount = userViewModels.Count(u => u.Role == "Sales Staff" || u.Role == "Sales Manager"),
                SupportTeamCount = userViewModels.Count(u => u.Role == "Support Staff"),
                ActiveUsers = userViewModels.Count // All users are considered active for now
            };

            return View(viewModel);
        }

        private string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        }

        private string GetLastActiveDisplay(DateTime? lastActive)
        {
            if (!lastActive.HasValue) return "Never";
            var timeAgo = DateTime.UtcNow - lastActive.Value;
            if (timeAgo.TotalMinutes < 60)
                return $"{(int)timeAgo.TotalMinutes} min ago";
            if (timeAgo.TotalHours < 24)
                return $"{(int)timeAgo.TotalHours} hours ago";
            return $"{(int)timeAgo.TotalDays} days ago";
        }

        public IActionResult ModuleManagement()
        {
            ViewData["CurrentPage"] = "Module Management";
            
            var viewModel = new ViewModels.ModuleManagementViewModel
            {
                Modules = new List<ViewModels.ModuleItemViewModel>
                {
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Customer Management",
                        Description = "Manage customer data and relationships",
                        Status = "Active",
                        ActiveUsers = _userManager.Users.Count(),
                        TotalRecords = _context.Customers.Count(),
                        LastUpdated = _context.Customers.Any() ? _context.Customers.Max(c => c.CreatedAt) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Sales Management",
                        Description = "Track leads, opportunities, and orders",
                        Status = "Active",
                        ActiveUsers = _userManager.Users.Count(u => u.Email.Contains("sales")),
                        TotalRecords = _context.Orders.Count(),
                        LastUpdated = _context.Orders.Any() ? _context.Orders.Max(o => o.OrderDate) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Support Tickets",
                        Description = "Customer support and ticket management",
                        Status = "Active",
                        ActiveUsers = _userManager.Users.Count(u => u.Email.Contains("support")),
                        TotalRecords = _context.SupportTickets.Count(),
                        LastUpdated = _context.SupportTickets.Any() ? _context.SupportTickets.Max(t => t.CreatedAt) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Marketing Campaigns",
                        Description = "Campaign management and analytics",
                        Status = "Active",
                        ActiveUsers = _userManager.Users.Count(u => u.Email.Contains("marketing")),
                        TotalRecords = _context.Campaigns.Count(),
                        LastUpdated = _context.Campaigns.Any() ? _context.Campaigns.Max(c => c.StartDate) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Billing & Invoices",
                        Description = "Invoice generation and payment tracking",
                        Status = "Active",
                        ActiveUsers = _userManager.Users.Count(u => u.Email.Contains("billing")),
                        TotalRecords = _context.Invoices.Count(),
                        LastUpdated = _context.Invoices.Any() ? _context.Invoices.Max(i => i.IssueDate) : DateTime.UtcNow
                    }
                }
            };
            
            return View(viewModel);
        }

        public async Task<IActionResult> Analytics()
        {
            ViewData["CurrentPage"] = "Analytics";
            
            var totalRevenue = _context.Orders.Where(o => o.Status == Models.OrderStatus.Completed).Sum(o => o.TotalAmount);
            var activeUsers = _userManager.Users.Count();
            var totalLeads = _context.Leads.Count();
            var convertedLeads = _context.Leads.Count(l => l.Status == "Converted");
            var conversionRate = totalLeads > 0 ? (double)convertedLeads / totalLeads * 100 : 0;
            
            var avgResponseTime = _context.SupportTickets.Any() 
                ? _context.SupportTickets
                    .Where(t => t.LastUpdated.HasValue)
                    .Average(t => (t.LastUpdated.Value - t.CreatedAt).TotalHours)
                : 0;

            var viewModel = new ViewModels.AnalyticsViewModel
            {
                TotalRevenue = totalRevenue,
                ActiveUsers = activeUsers,
                ConversionRate = conversionRate,
                AvgResponseTimeHours = avgResponseTime,
                RevenueGrowth = 15.3, // Placeholder - would need historical data
                UserGrowth = 8.2,
                ConversionGrowth = 3.1,
                ResponseTimeImprovement = -12.4,
                TopPerformingModule = "Customer Master Data",
                TopModuleUsers = _context.Customers.Count(),
                BestConversionModule = "Sales Management",
                BestConversionRate = conversionRate,
                FastestResponseModule = "Customer Support",
                FastestResponseTime = avgResponseTime
            };

            // Weekly ticket trends (last 7 days) - REAL DATA
            var today = DateTime.UtcNow.Date;
            var daysOfWeek = new[] { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };
            
            for (int i = 0; i < 7; i++)
            {
                var targetDate = today.AddDays(-6 + i);
                var dayName = daysOfWeek[i];
                
                var ticketsOnDay = await _context.SupportTickets
                    .Where(t => t.CreatedAt.Date == targetDate)
                    .ToListAsync();
                
                viewModel.WeeklyTicketTrends[dayName] = new ViewModels.TicketTrendData
                {
                    Closed = ticketsOnDay.Count(t => t.Status == "Resolved" || t.Status == "Closed"),
                    Opened = ticketsOnDay.Count(t => t.Status == "Open" || t.Status == "New"),
                    Pending = ticketsOnDay.Count(t => t.Status == "In Progress" || t.Status == "Pending")
                };
            }

            // Monthly lead trends (last 6 months) - REAL DATA
            var monthNames = new[] { "Jan", "Feb", "Mar", "Apr", "May", "Jun" };
            for (int i = 0; i < 6; i++)
            {
                var targetMonth = DateTime.UtcNow.AddMonths(-5 + i);
                var monthName = monthNames[i];
                
                var leadsInMonth = await _context.Leads
                    .Where(l => l.CreatedAt.Year == targetMonth.Year && l.CreatedAt.Month == targetMonth.Month)
                    .CountAsync();
                
                viewModel.MonthlyLeadTrends[monthName] = leadsInMonth;
            }

            return View(viewModel);
        }

        public async Task<IActionResult> AuditLog()
        {
            ViewData["CurrentPage"] = "Audit Log";
            
            var auditLogs = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(50)
                .ToListAsync();
            
            return View(auditLogs);
        }

        // GET: Admin/CreateUser
        [Authorize(Roles = "Super Admin")]
        public IActionResult CreateUser()
        {
            ViewData["CurrentPage"] = "User Management";
            return View();
        }

        // POST: Admin/CreateUser
        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string password, string firstName, string lastName, string role)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(role))
            {
                ModelState.AddModelError("", "Email, password, and role are required.");
                return View();
            }

            var user = new Models.ApplicationUser
            {
                UserName = email,
                Email = email,
                FirstName = firstName,
                LastName = lastName,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, password);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, role);
                TempData["SuccessMessage"] = $"User {email} created successfully!";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }

        // GET: Admin/EditUser/5
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["CurrentPage"] = "User Management";
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "No Role";
            return View(user);
        }

        // POST: Admin/EditUser/5
        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(string id, string firstName, string lastName, string companyName, string email, string role)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = firstName;
            user.LastName = lastName;
            user.CompanyName = companyName;
            user.Email = email;
            user.UserName = email;

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                // Update role
                var currentRoles = await _userManager.GetRolesAsync(user);
                if (currentRoles.Any())
                {
                    await _userManager.RemoveFromRolesAsync(user, currentRoles);
                }
                if (!string.IsNullOrWhiteSpace(role))
                {
                    await _userManager.AddToRoleAsync(user, role);
                }

                TempData["SuccessMessage"] = $"User {email} updated successfully!";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "No Role";
            return View(user);
        }

        // GET: Admin/DeleteUser/5
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var roles = await _userManager.GetRolesAsync(user);
            ViewData["CurrentPage"] = "User Management";
            ViewBag.UserRole = roles.FirstOrDefault() ?? "No Role";
            return View(user);
        }

        // POST: Admin/DeleteUser/5
        [Authorize(Roles = "Super Admin")]
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = $"User deleted successfully!";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View(user);
        }

        // System Settings - Super Admin Only
        [Authorize(Roles = "Super Admin")]
        public IActionResult SystemSettings()
        {
            ViewData["CurrentPage"] = "System Settings";
            return View();
        }

        // Reset Password - Super Admin Only
        [Authorize(Roles = "Super Admin")]
        public async Task<IActionResult> ResetPassword(string? userId = null)
        {
            ViewData["CurrentPage"] = "ResetPassword";

            var users = _userManager.Users.ToList();
            var userList = new List<(string Id, string DisplayName)>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = u.UserName ?? u.Email ?? "Unknown";
                var role = roles.FirstOrDefault() ?? "No Role";
                userList.Add((u.Id, $"{fullName} ({u.Email}) — {role}"));
            }

            ViewBag.UserList = userList;
            ViewBag.SelectedUserId = userId;
            return View();
        }

        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword, string confirmPassword)
        {
            ViewData["CurrentPage"] = "ResetPassword";

            // Reload user list for re-display on error
            var users = _userManager.Users.ToList();
            var userList = new List<(string Id, string DisplayName)>();
            foreach (var u in users)
            {
                var roles = await _userManager.GetRolesAsync(u);
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = u.UserName ?? u.Email ?? "Unknown";
                var role = roles.FirstOrDefault() ?? "No Role";
                userList.Add((u.Id, $"{fullName} ({u.Email}) — {role}"));
            }
            ViewBag.UserList = userList;
            ViewBag.SelectedUserId = userId;

            if (string.IsNullOrWhiteSpace(userId))
            {
                ModelState.AddModelError("", "Please select a user.");
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword) || newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Passwords do not match or are empty.");
                return View();
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                ModelState.AddModelError("", "User not found.");
                return View();
            }

            // Remove existing password then add new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = user.Email ?? "User";
                TempData["SuccessMessage"] = $"Password for {fullName} has been reset successfully.";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }
    }
}
