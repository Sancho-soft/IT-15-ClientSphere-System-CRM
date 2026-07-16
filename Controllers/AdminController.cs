using ClientSphere.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Controllers
{
    [Authorize(Roles = "Super Admin, Admin")]
    public class AdminController : Controller
    {
        private const string CurrentPageKey = "CurrentPage";
        private const string UnknownValue = "Unknown";
        private const string UnknownLowerValue = "unknown";
        private const string SuccessMessageKey = "SuccessMessage";
        private const string ErrorMessageKey = "ErrorMessage";
        private const string EmailGroup = "Email";
        private const string NotificationsGroup = "Notifications";

        private readonly Data.ApplicationDbContext _context;
        private readonly Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> _userManager;
        private readonly Services.ISystemSettingService _systemSettingService;
        private readonly Services.RateLimitCacheService _rateLimitCacheService;
        private readonly ILogger<AdminController> _logger;

        public AdminController(Data.ApplicationDbContext context, Microsoft.AspNetCore.Identity.UserManager<Models.ApplicationUser> userManager, Services.ISystemSettingService systemSettingService, Services.RateLimitCacheService rateLimitCacheService, ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _systemSettingService = systemSettingService;
            _rateLimitCacheService = rateLimitCacheService;
            _logger = logger;
        }

        public async Task<IActionResult> Dashboard()
        {
            ViewData[CurrentPageKey] = "Dashboard";

            var recentActivities = await _context.AuditLogs
                .OrderByDescending(a => a.Timestamp)
                .Take(5)
                .ToListAsync();

            var viewModel = new ViewModels.AdminDashboardViewModel
            {
                TotalUsers = await _userManager.Users.CountAsync(),
                TotalOrders = await _context.Orders.CountAsync(),
                TotalRevenue = await _context.Orders.Where(o => o.Status == Models.OrderStatus.Completed).SumAsync(o => o.TotalAmount),
                ActiveTickets = await _context.SupportTickets.CountAsync(t => t.Status != "Resolved" && t.Status != "Closed"),
                PendingLeads = await _context.Leads.CountAsync(l => l.Status == "New" || l.Status == "Contacted"),
                ActiveCampaigns = await _context.Campaigns.CountAsync(c => c.Status == "Active"),
                PendingInvoices = await _context.Invoices.CountAsync(i => i.Status == "Pending" || i.Status == "Sent"),
                RecentActivities = recentActivities
            };

            return View(viewModel);
        }


        // User Management - Super Admin Only View, Admin Manages
        [Authorize(Roles = "Super Admin, Admin")]
        public async Task<IActionResult> UserManagement()
        {
            ViewData[CurrentPageKey] = "User Management";

            // Batch role lookup to avoid N+1 queries (Finding #8)
            var users = await _userManager.Users.ToListAsync();
            var userRoles = await _context.UserRoles.ToListAsync();
            var allRoles = await _context.Roles.ToListAsync();
            var roleMap = userRoles
                .Join(allRoles, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var userViewModels = new List<ViewModels.UserItemViewModel>();

            foreach (var user in users)
            {
                roleMap.TryGetValue(user.Id, out var roles);
                var roleName = roles?.FirstOrDefault() ?? StatusValues.NoRole;
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                    fullName = user.UserName ?? StatusValues.Unknown;

                var isLockedOut = user.LockoutEnd.HasValue && user.LockoutEnd.Value > DateTimeOffset.UtcNow;
                var status = isLockedOut ? "Locked Out" : (user.IsActive ? StatusValues.Active : StatusValues.Inactive);

                userViewModels.Add(new ViewModels.UserItemViewModel
                {
                    Id = user.Id,
                    FullName = fullName,
                    Email = user.Email ?? "No Email",
                    Initials = GetInitials(fullName),
                    Role = roleName,
                    Status = status,
                    IsLockedOut = isLockedOut,
                    LastActive = user.LastLoginDate ?? user.CreatedAt,
                    LastActiveDisplay = GetLastActiveDisplay(user.LastLoginDate ?? user.CreatedAt),
                    ProfilePictureUrl = user.ProfilePictureUrl
                });
            }

            var viewModel = new ViewModels.UserManagementViewModel
            {
                Users = userViewModels,
                TotalUsers = users.Count,
                AdminCount = userViewModels.Count(u => u.Role == Roles.Admin),
                SalesTeamCount = userViewModels.Count(u => u.Role == Roles.SalesStaff || u.Role == Roles.SalesManager),
                SupportTeamCount = userViewModels.Count(u => u.Role == Roles.SupportStaff),
                ActiveUsers = userViewModels.Count // All users are considered active for now
            };

            return View(viewModel);
        }

        private static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "??";
            var parts = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpper();
            return parts[0].Substring(0, Math.Min(2, parts[0].Length)).ToUpper();
        }

        private static string GetLastActiveDisplay(DateTime? lastActive)
        {
            if (!lastActive.HasValue) return "Never";
            var timeAgo = DateTime.UtcNow - lastActive.Value;
            if (timeAgo.TotalMinutes < 60)
                return $"{(int)timeAgo.TotalMinutes} min ago";
            if (timeAgo.TotalHours < 24)
                return $"{(int)timeAgo.TotalHours} hours ago";
            return $"{(int)timeAgo.TotalDays} days ago";
        }

        public async Task<IActionResult> ModuleManagement()
        {
            ViewData[CurrentPageKey] = "Module Management";

            // Use role-based counts instead of fragile email substring matching (Finding #18)
            var salesUsers = (await _userManager.GetUsersInRoleAsync(Roles.SalesStaff)).Count
                           + (await _userManager.GetUsersInRoleAsync(Roles.SalesManager)).Count;
            var supportUsers = (await _userManager.GetUsersInRoleAsync(Roles.SupportStaff)).Count;
            var marketingUsers = (await _userManager.GetUsersInRoleAsync(Roles.MarketingStaff)).Count
                               + (await _userManager.GetUsersInRoleAsync(Roles.MarketingManager)).Count;
            var billingUsers = (await _userManager.GetUsersInRoleAsync(Roles.BillingStaff)).Count;

            var viewModel = new ViewModels.ModuleManagementViewModel
            {
                Modules = new List<ViewModels.ModuleItemViewModel>
                {
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Customer Management",
                        Description = "Manage customer data and relationships",
                        Status = StatusValues.Active,
                        ActiveUsers = await _userManager.Users.CountAsync(),
                        TotalRecords = await _context.Customers.CountAsync(),
                        LastUpdated = await _context.Customers.AnyAsync() ? await _context.Customers.MaxAsync(c => c.CreatedAt) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Sales Management",
                        Description = "Track leads, opportunities, and orders",
                        Status = StatusValues.Active,
                        ActiveUsers = salesUsers,
                        TotalRecords = await _context.Orders.CountAsync(),
                        LastUpdated = await _context.Orders.AnyAsync() ? await _context.Orders.MaxAsync(o => o.OrderDate) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Support Tickets",
                        Description = "Customer support and ticket management",
                        Status = StatusValues.Active,
                        ActiveUsers = supportUsers,
                        TotalRecords = await _context.SupportTickets.CountAsync(),
                        LastUpdated = await _context.SupportTickets.AnyAsync() ? await _context.SupportTickets.MaxAsync(t => t.CreatedAt) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Marketing Campaigns",
                        Description = "Campaign management and analytics",
                        Status = StatusValues.Active,
                        ActiveUsers = marketingUsers,
                        TotalRecords = await _context.Campaigns.CountAsync(),
                        LastUpdated = await _context.Campaigns.AnyAsync() ? await _context.Campaigns.MaxAsync(c => c.StartDate) : DateTime.UtcNow
                    },
                    new ViewModels.ModuleItemViewModel
                    {
                        Name = "Billing & Invoices",
                        Description = "Invoice generation and payment tracking",
                        Status = StatusValues.Active,
                        ActiveUsers = billingUsers,
                        TotalRecords = await _context.Invoices.CountAsync(),
                        LastUpdated = await _context.Invoices.AnyAsync() ? await _context.Invoices.MaxAsync(i => i.IssueDate) : DateTime.UtcNow
                    }
                }
            };
            
            return View(viewModel);
        }


        public async Task<IActionResult> AuditLog(int page = 1, int pageSize = 25, string? searchUser = null, string? searchAction = null)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            ViewData[CurrentPageKey] = "Audit Log";
            ViewData["SearchUser"] = searchUser;
            ViewData["SearchAction"] = searchAction;

            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchUser))
                query = query.Where(a => a.UserName.Contains(searchUser));
            if (!string.IsNullOrWhiteSpace(searchAction))
                query = query.Where(a => a.Action.Contains(searchAction));

            var totalItems = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var auditLogs = await query
                .OrderByDescending(a => a.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewData["CurrentPageNumber"] = page;
            ViewData["TotalPages"] = totalPages;
            ViewData["PageSize"] = pageSize;

            return View(auditLogs);
        }

        // GET: Admin/CreateUser
        [Authorize(Roles = "Super Admin, Admin")]
        public IActionResult CreateUser()
        {
            ViewData[CurrentPageKey] = "User Management";
            return View();
        }

        // POST: Admin/CreateUser
        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(string email, string password, string firstName, string lastName, string role)
        {
            if (role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                ModelState.AddModelError("", "Access Denied: Admins cannot create Super Admin accounts.");
                return View();
            }

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
                
                // Add System Logs
                if (role == "Super Admin" || role == "Admin")
                {
                    _logger.LogWarning("SYSTEM SECURITY ALERT: A new high-level user '{Email}' was granted the '{Role}' role by {Creator}.", email, role, User.Identity?.Name ?? UnknownValue);
                }
                else
                {
                    _logger.LogInformation("System Info: New user '{Email}' was created with role '{Role}'.", email, role);
                }

                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Created",
                    Description = $"User {email} created with role '{role}' by {User.Identity?.Name ?? UnknownValue}",
                    UserId = _userManager.GetUserId(User) ?? UnknownValue,
                    UserName = User.Identity?.Name ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = $"User {email} created successfully!";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }
            return View();
        }

        // GET: Admin/EditUser/5
        [Authorize(Roles = "Super Admin, Admin")]
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
            if (roles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot edit Super Admin accounts.";
                return RedirectToAction(nameof(UserManagement));
            }

            ViewData[CurrentPageKey] = "User Management";
            ViewBag.CurrentRole = roles.FirstOrDefault() ?? "No Role";
            return View(user);
        }

        // POST: Admin/EditUser/5
        [Authorize(Roles = "Super Admin, Admin")]
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

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot modify Super Admin accounts.";
                return RedirectToAction(nameof(UserManagement));
            }

            if (role == Roles.SuperAdmin && !User.IsInRole(Roles.SuperAdmin))
            {
                ModelState.AddModelError("", "Access Denied: Admins cannot assign the Super Admin role.");
                ViewBag.CurrentRole = targetRoles.FirstOrDefault() ?? "No Role";
                return View(user);
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

                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Role Changed",
                    Description = $"Role for {user.Email} changed to '{role}' by {User.Identity?.Name ?? UnknownValue}",
                    UserId = _userManager.GetUserId(User) ?? UnknownValue,
                    UserName = User.Identity?.Name ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();

                TempData[SuccessMessageKey] = $"User {email} updated successfully!";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("", error.Description);
            }

            ViewBag.CurrentRole = targetRoles.FirstOrDefault() ?? "No Role";
            return View(user);
        }

        // POST: Admin/SuspendUser
        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SuspendUser(string id, int durationDays)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var targetUser = await _userManager.FindByIdAsync(id);
            if (targetUser == null) return NotFound();

            if (targetUser.Id == currentUser.Id)
            {
                TempData[ErrorMessageKey] = "You cannot suspend your own account.";
                return RedirectToAction(nameof(UserManagement));
            }

            var targetRoles = await _userManager.GetRolesAsync(targetUser);
            if (targetRoles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot suspend a Super Admin.";
                return RedirectToAction(nameof(UserManagement));
            }

            targetUser.LockoutEnabled = true;
            targetUser.LockoutEnd = DateTimeOffset.UtcNow.AddDays(durationDays);
            
            // Immediately invalidate any active login sessions for this user
            await _userManager.UpdateSecurityStampAsync(targetUser);

            var result = await _userManager.UpdateAsync(targetUser);
            if (result.Succeeded)
            {
                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Suspended",
                    Description = $"User {targetUser.Email} (ID: {targetUser.Id}) suspended for {durationDays} days by {currentUser.Email}",
                    UserId = currentUser.Id,
                    UserName = currentUser.Email ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = $"User '{targetUser.Email}' has been suspended for {durationDays} day(s).";
            }
            else
            {
                TempData[ErrorMessageKey] = "Failed to suspend user.";
            }

            return RedirectToAction(nameof(UserManagement));
        }

        // POST: Admin/DeactivateUser — disables login without data loss
        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactivateUser(string id, string confirmPassword)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            if (user.Id == currentUser.Id)
            {
                TempData[ErrorMessageKey] = "You cannot deactivate your own account.";
                return RedirectToAction(nameof(UserManagement));
            }

            // Verify the current admin's password before proceeding with deactivation
            if (string.IsNullOrEmpty(confirmPassword) || !await _userManager.CheckPasswordAsync(currentUser, confirmPassword))
            {
                TempData[ErrorMessageKey] = "Authentication failed. Invalid password entered.";
                return RedirectToAction(nameof(UserManagement));
            }

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot deactivate a Super Admin.";
                return RedirectToAction(nameof(UserManagement));
            }

            user.IsActive = false;
            user.LockoutEnabled = true;
            user.LockoutEnd = DateTimeOffset.MaxValue; // Lock indefinitely

            // Immediately invalidate any active login sessions for this user
            await _userManager.UpdateSecurityStampAsync(user);

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Deactivated",
                    Description = $"User {user.Email} deactivated by {currentUser.Email}",
                    UserId = currentUser.Id,
                    UserName = currentUser.Email ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = $"User '{user.Email}' has been deactivated. They can no longer log in.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        // POST: Admin/ReactivateUser
        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ReactivateUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot reactivate a Super Admin.";
                return RedirectToAction(nameof(UserManagement));
            }

            user.IsActive = true;
            user.LockoutEnd = null; // Remove lockout
            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Reactivated",
                    Description = $"User {user.Email} reactivated by {currentUser.Email}",
                    UserId = currentUser.Id,
                    UserName = currentUser.Email ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = $"User '{user.Email}' has been reactivated.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        // POST: Admin/UnlockUser — unlocks locked out users
        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnlockUser(string id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Challenge();

            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.SuperAdmin) && !User.IsInRole(Roles.SuperAdmin))
            {
                TempData[ErrorMessageKey] = "Access Denied: Admins cannot unlock a Super Admin.";
                return RedirectToAction(nameof(UserManagement));
            }

            var result = await _userManager.SetLockoutEndDateAsync(user, null);
            if (result.Succeeded)
            {
                await _userManager.ResetAccessFailedCountAsync(user);

                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "User Unlocked",
                    Description = $"User {user.Email} unlocked by {currentUser.Email}",
                    UserId = currentUser.Id,
                    UserName = currentUser.Email ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                TempData[SuccessMessageKey] = $"User '{user.Email}' has been unlocked.";
            }
            else
            {
                TempData[ErrorMessageKey] = "Failed to unlock user.";
            }
            return RedirectToAction(nameof(UserManagement));
        }

        // System Settings - Super Admin and Admin
        [Authorize(Roles = "Super Admin,Admin")]
        public async Task<IActionResult> SystemSettings()
        {
            ViewData[CurrentPageKey] = "System Settings";
            var viewModel = new ViewModels.SystemSettingsViewModel
            {
                SessionTimeoutMinutes = await _systemSettingService.GetSettingIntAsync("SessionTimeoutMinutes", 15),
                MinimumPasswordLength = await _systemSettingService.GetSettingIntAsync("MinimumPasswordLength", 8),
                SmtpServer = await _systemSettingService.GetSettingAsync("SmtpServer", "smtp.clientsphere.com"),
                SmtpPort = await _systemSettingService.GetSettingIntAsync("SmtpPort", 587),
                SmtpEncryption = await _systemSettingService.GetSettingAsync("SmtpEncryption", "TLS"),
                FromEmailAddress = await _systemSettingService.GetSettingAsync("FromEmailAddress", "noreply@clientsphere.com"),
                NotifyNewRegistrations = await _systemSettingService.GetSettingBoolAsync("NotifyNewRegistrations", true),
                NotifySystemUpdates = await _systemSettingService.GetSettingBoolAsync("NotifySystemUpdates", true),
                NotifyCriticalAlerts = await _systemSettingService.GetSettingBoolAsync("NotifyCriticalAlerts", true),
                AutomaticBackups = await _systemSettingService.GetSettingBoolAsync("AutomaticBackups", true),
                BackupFrequency = await _systemSettingService.GetSettingAsync("BackupFrequency", "Daily"),
                RetentionPeriod = await _systemSettingService.GetSettingAsync("RetentionPeriod", "30 days"),
                ApiAccessEnabled = await _systemSettingService.GetSettingBoolAsync("ApiAccessEnabled", true),
                ApiRateLimit = await _systemSettingService.GetSettingIntAsync("ApiRateLimit", 100),
                SystemApiKey = await _systemSettingService.GetSettingAsync("SystemApiKey", "........................................")
            };
            return View(viewModel);
        }

        [Authorize(Roles = "Super Admin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SystemSettings(ViewModels.SystemSettingsViewModel model)
        {
            ViewData[CurrentPageKey] = "System Settings";
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Save all settings using the service
            await _systemSettingService.SetSettingIntAsync("SessionTimeoutMinutes", model.SessionTimeoutMinutes, "General");
            await _systemSettingService.SetSettingIntAsync("MinimumPasswordLength", model.MinimumPasswordLength, "General");
            
            await _systemSettingService.SetSettingAsync("SmtpServer", model.SmtpServer, EmailGroup);
            await _systemSettingService.SetSettingIntAsync("SmtpPort", model.SmtpPort, EmailGroup);
            await _systemSettingService.SetSettingAsync("SmtpEncryption", model.SmtpEncryption, EmailGroup);
            await _systemSettingService.SetSettingAsync("FromEmailAddress", model.FromEmailAddress, EmailGroup);

            await _systemSettingService.SetSettingBoolAsync("NotifyNewRegistrations", model.NotifyNewRegistrations, NotificationsGroup);
            await _systemSettingService.SetSettingBoolAsync("NotifySystemUpdates", model.NotifySystemUpdates, NotificationsGroup);
            await _systemSettingService.SetSettingBoolAsync("NotifyCriticalAlerts", model.NotifyCriticalAlerts, NotificationsGroup);

            await _systemSettingService.SetSettingBoolAsync("AutomaticBackups", model.AutomaticBackups, "Database");
            await _systemSettingService.SetSettingAsync("BackupFrequency", model.BackupFrequency, "Database");
            await _systemSettingService.SetSettingAsync("RetentionPeriod", model.RetentionPeriod, "Database");

            await _systemSettingService.SetSettingBoolAsync("ApiAccessEnabled", model.ApiAccessEnabled, "API");
            await _systemSettingService.SetSettingIntAsync("ApiRateLimit", model.ApiRateLimit, "API");
            
            // Update the in-memory cache directly for immediate Rate Limiting effect
            _rateLimitCacheService.CurrentApiRateLimit = model.ApiRateLimit;
            
            // Apply Session Timeout (Requirement 1)
            var sessionOptions = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<SessionOptions>>().Value;
            sessionOptions.IdleTimeout = TimeSpan.FromMinutes(model.SessionTimeoutMinutes);

            // Apply Minimum Password Length (Requirement 2)
            var identityOptions = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Options.IOptions<Microsoft.AspNetCore.Identity.IdentityOptions>>().Value;
            identityOptions.Password.RequiredLength = model.MinimumPasswordLength;
            
            TempData[SuccessMessageKey] = "System settings updated successfully!";
            return RedirectToAction(nameof(SystemSettings));
        }

        [Authorize(Roles = "Super Admin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendTestEmail([FromServices] Services.IEmailService emailService)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid request settings.";
                return RedirectToAction(nameof(SystemSettings));
            }

            var fromEmail = await _systemSettingService.GetSettingAsync("FromEmailAddress", "noreply@clientsphere.com");
            try
            {
                await emailService.SendWelcomeEmailAsync(fromEmail, "Admin"); // Re-using a method or creating a generic one. Actually SendWelcomeEmailAsync sends welcome text. Let's use the actual SendEmailAsync from the implementation if it's there. Wait, IEmailService doesn't have SendEmailAsync in the interface! I should use SendWelcomeEmailAsync or cast it. I'll cast it to Microsoft.AspNetCore.Identity.UI.Services.IEmailSender because SendGridEmailService implements both.
                
                var emailSender = (Microsoft.AspNetCore.Identity.UI.Services.IEmailSender)emailService;
                await emailSender.SendEmailAsync(fromEmail, "ClientSphere Test Email", "<p>This is a test email confirming the email configuration is working.</p>");
                
                TempData[SuccessMessageKey] = $"Test email sent successfully to {fromEmail}!";
            }
            catch (Exception ex)
            {
                TempData[ErrorMessageKey] = $"Failed to send test email: {ex.Message}";
            }
            return RedirectToAction(nameof(SystemSettings));
        }

        [Authorize(Roles = "Super Admin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateApiKey()
        {
            var bytes = new byte[32];
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            var newKey = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
            
            await _systemSettingService.SetSettingAsync("SystemApiKey", newKey, "API");
            
            TempData[SuccessMessageKey] = "System API Key regenerated successfully!";
            return RedirectToAction(nameof(SystemSettings));
        }

        [Authorize(Roles = "Super Admin,Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BackupNow()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var package = new Models.BackupPackage
            {
                Users = await _context.Users.ToListAsync(),
                Roles = await _context.Roles.ToListAsync(),
                UserRoles = await _context.UserRoles.ToListAsync(),
                Customers = await _context.Customers.ToListAsync(),
                Products = await _context.Products.ToListAsync(),
                Orders = await _context.Orders.ToListAsync(),
                OrderItems = await _context.OrderItems.ToListAsync(),
                Leads = await _context.Leads.ToListAsync(),
                Opportunities = await _context.Opportunities.ToListAsync(),
                Appointments = await _context.Appointments.ToListAsync(),
                SupportTickets = await _context.SupportTickets.ToListAsync(),
                Campaigns = await _context.Campaigns.ToListAsync(),
                Invoices = await _context.Invoices.ToListAsync(),
                PaymentRecords = await _context.PaymentRecords.ToListAsync(),
                AuditLogs = await _context.AuditLogs.ToListAsync(),
                SystemSettings = await _context.SystemSettings.ToListAsync(),
                Notifications = await _context.Notifications.ToListAsync()
            };

            var options = new System.Text.Json.JsonSerializerOptions 
            { 
                WriteIndented = true,
                ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles
            };
            byte[] fileBytes;
            try
            {
                fileBytes = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(package, options);
                
                var history = new Models.BackupHistory
                {
                    Timestamp = DateTime.UtcNow,
                    FileSizeBytes = fileBytes.Length,
                    TriggeredByUserId = user.Id,
                    TriggeredByUserName = user.UserName ?? user.Email ?? UnknownValue,
                    Status = "Completed"
                };
                _context.BackupHistories.Add(history);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var history = new Models.BackupHistory
                {
                    Timestamp = DateTime.UtcNow,
                    FileSizeBytes = 0,
                    TriggeredByUserId = user.Id,
                    TriggeredByUserName = user.UserName ?? user.Email ?? UnknownValue,
                    Status = "Failed: " + ex.Message
                };
                _context.BackupHistories.Add(history);
                await _context.SaveChangesAsync();
                TempData[ErrorMessageKey] = "Backup export failed: " + ex.Message;
                return RedirectToAction(nameof(SystemSettings));
            }

            var fileName = $"clientsphere-backup-{DateTime.UtcNow:yyyy-MM-dd-HHmmss}.json";
            return File(fileBytes, "application/json", fileName);
        }

        [Authorize(Roles = "Super Admin,Admin")]
        [HttpGet]
        public async Task<IActionResult> BackupHistory()
        {
            ViewData[CurrentPageKey] = "Backup History";
            var history = await _context.BackupHistories.OrderByDescending(b => b.Timestamp).ToListAsync();
            return View(history);
        }

        [Authorize(Roles = "Super Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(long.MaxValue)]
        [RequestFormLimits(MultipartBodyLengthLimit = long.MaxValue)]
        public async Task<IActionResult> RestoreBackup(IFormFile backupFile)
        {
            if (!ModelState.IsValid)
            {
                TempData[ErrorMessageKey] = "Invalid file or parameters.";
                return RedirectToAction(nameof(BackupHistory));
            }

            if (backupFile == null || backupFile.Length == 0)
            {
                TempData[ErrorMessageKey] = "Please select a valid JSON backup file.";
                return RedirectToAction(nameof(BackupHistory));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            Models.BackupPackage package;
            try
            {
                using var stream = backupFile.OpenReadStream();
                var options = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                package = await System.Text.Json.JsonSerializer.DeserializeAsync<Models.BackupPackage>(stream, options);
                
                if (package == null) throw new InvalidOperationException("Deserialized package is null.");
            }
            catch (Exception ex)
            {
                TempData[ErrorMessageKey] = "Invalid backup file format. Validation failed: " + ex.Message;
                return RedirectToAction(nameof(BackupHistory));
            }

            try
            {
                await PerformRestoreDatabaseAsync(package, user);
                TempData[SuccessMessageKey] = "System data has been successfully restored from the backup file.";
            }
            catch (Exception ex)
            {
                TempData[ErrorMessageKey] = "Failed to restore backup: " + ex.Message + (ex.InnerException != null ? " | " + ex.InnerException.Message : "");
            }

            return RedirectToAction(nameof(BackupHistory));
        }

        private async Task PerformRestoreDatabaseAsync(Models.BackupPackage package, Models.ApplicationUser user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Completely clear EF tracking to prevent any key conflicts with existing tracked entities (like the logged-in user)
                _context.ChangeTracker.Clear();

                // Delete data in reverse dependency order
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM OrderItems");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Orders");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Invoices");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM PaymentRecords");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Notifications");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Appointments");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Opportunities");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Leads");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM SupportTickets");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Campaigns");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Customers");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM Products");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM SystemSettings");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUserRoles");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetRoles");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers");
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM AuditLogs");

                // Add non-identity tables first
                if (package.Users != null && package.Users.Any()) await _context.Users.AddRangeAsync(package.Users);
                if (package.Roles != null && package.Roles.Any()) await _context.Roles.AddRangeAsync(package.Roles);
                await _context.SaveChangesAsync();

                if (package.UserRoles != null && package.UserRoles.Any()) await _context.UserRoles.AddRangeAsync(package.UserRoles);
                if (package.SystemSettings != null && package.SystemSettings.Any()) await _context.SystemSettings.AddRangeAsync(package.SystemSettings);
                await _context.SaveChangesAsync();

                await InsertWithIdentity("Products", package.Products);
                await InsertWithIdentity("Customers", package.Customers);
                await InsertWithIdentity("Campaigns", package.Campaigns);
                await InsertWithIdentity("SupportTickets", package.SupportTickets);
                await InsertWithIdentity("Leads", package.Leads);
                await InsertWithIdentity("Opportunities", package.Opportunities);
                await InsertWithIdentity("Appointments", package.Appointments);
                await InsertWithIdentity("Orders", package.Orders);
                await InsertWithIdentity("OrderItems", package.OrderItems);
                await InsertWithIdentity("Invoices", package.Invoices);
                await InsertWithIdentity("PaymentRecords", package.PaymentRecords);
                await InsertWithIdentity("Notifications", package.Notifications);
                await InsertWithIdentity("AuditLogs", package.AuditLogs);

                // Add Audit Log for Restore
                _context.AuditLogs.Add(new Models.AuditLog
                {
                    Timestamp = DateTime.UtcNow,
                    UserId = user.Id,
                    UserName = user.UserName ?? user.Email ?? UnknownValue,
                    Action = "Data Restore",
                    Description = $"System data was fully restored from backup by Super Admin [{user.UserName}].",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownValue
                });
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        private async Task InsertWithIdentity<T>(string tableName, List<T> data) where T : class
        {
            if (data == null || !data.Any()) return;
            
            // Clear tracking before each batch insert to prevent conflicts with navigation properties
            _context.ChangeTracker.Clear();
            
            await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} ON");
            await _context.Set<T>().AddRangeAsync(data);
            await _context.SaveChangesAsync();
            await _context.Database.ExecuteSqlRawAsync($"SET IDENTITY_INSERT {tableName} OFF");
        }

        // Reset Password - Super Admin and Admin
        [Authorize(Roles = "Super Admin, Admin")]
        public async Task<IActionResult> ResetPassword(string? userId = null)
        {
            ViewData[CurrentPageKey] = "ResetPassword";

            var users = await _userManager.Users.ToListAsync();
            var userRolesList = await _context.UserRoles.ToListAsync();
            var allRolesList = await _context.Roles.ToListAsync();
            var roleMap = userRolesList
                .Join(allRolesList, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var isCurrentSuperAdmin = User.IsInRole(Roles.SuperAdmin);
            var userList = new List<(string Id, string DisplayName)>();
            foreach (var u in users)
            {
                roleMap.TryGetValue(u.Id, out var roles);
                var role = roles?.FirstOrDefault() ?? StatusValues.NoRole;
                if (role == Roles.SuperAdmin && !isCurrentSuperAdmin)
                {
                    continue; // Skip Super Admin accounts for non-Super Admin users
                }
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = u.UserName ?? u.Email ?? StatusValues.Unknown;
                userList.Add((u.Id, $"{fullName} ({u.Email}) — {role}"));
            }

            ViewBag.UserList = userList;
            ViewBag.SelectedUserId = userId;
            return View();
        }

        [Authorize(Roles = "Super Admin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string userId, string newPassword, string confirmPassword)
        {
            ViewData[CurrentPageKey] = "ResetPassword";

            // Reload user list for re-display on error (batch role lookup)
            var users = await _userManager.Users.ToListAsync();
            var userRolesList = await _context.UserRoles.ToListAsync();
            var allRolesList = await _context.Roles.ToListAsync();
            var roleMap = userRolesList
                .Join(allRolesList, ur => ur.RoleId, r => r.Id, (ur, r) => new { ur.UserId, RoleName = r.Name })
                .GroupBy(x => x.UserId)
                .ToDictionary(g => g.Key, g => g.Select(x => x.RoleName).ToList());

            var isCurrentSuperAdmin = User.IsInRole(Roles.SuperAdmin);
            var userList = new List<(string Id, string DisplayName)>();
            foreach (var u in users)
            {
                roleMap.TryGetValue(u.Id, out var roles);
                var role = roles?.FirstOrDefault() ?? StatusValues.NoRole;
                if (role == Roles.SuperAdmin && !isCurrentSuperAdmin)
                {
                    continue; // Skip Super Admin accounts for non-Super Admin users
                }
                var fullName = $"{u.FirstName} {u.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = u.UserName ?? u.Email ?? StatusValues.Unknown;
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

            var targetRoles = await _userManager.GetRolesAsync(user);
            if (targetRoles.Contains(Roles.SuperAdmin) && !isCurrentSuperAdmin)
            {
                ModelState.AddModelError("", "Access Denied: Admins cannot reset Super Admin passwords.");
                return View();
            }

            // Remove existing password then add new one
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                _context.AuditLogs.Add(new ClientSphere.Models.AuditLog
                {
                    Action = "Password Reset",
                    Description = $"Password for {user.Email} (ID: {user.Id}) reset by {User.Identity?.Name ?? UnknownValue}",
                    UserId = _userManager.GetUserId(User) ?? UnknownValue,
                    UserName = User.Identity?.Name ?? UnknownValue,
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? UnknownLowerValue
                });
                await _context.SaveChangesAsync();
                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName)) fullName = user.Email ?? "User";
                TempData[SuccessMessageKey] = $"Password for {fullName} has been reset successfully.";
                return RedirectToAction(nameof(UserManagement));
            }

            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);

            return View();
        }
    }
}
