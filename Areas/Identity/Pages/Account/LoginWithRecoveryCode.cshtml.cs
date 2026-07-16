using System.ComponentModel.DataAnnotations;
using ClientSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientSphere.Areas.Identity.Pages.Account
{
    public class LoginWithRecoveryCodeModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginWithRecoveryCodeModel> _logger;
        private readonly ClientSphere.Data.ApplicationDbContext _context;

        public LoginWithRecoveryCodeModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginWithRecoveryCodeModel> logger,
            ClientSphere.Data.ApplicationDbContext context)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _context = context;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public string ReturnUrl { get; set; } = string.Empty;

        public class InputModel
        {
            [BindProperty]
            [Required]
            [DataType(DataType.Text)]
            [Display(Name = "Recovery Code")]
            public string RecoveryCode { get; set; } = string.Empty;
        }

        public async Task<IActionResult> OnGetAsync(string? returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("MFA Recovery: Two-factor user was null, redirecting to Login.");
                return RedirectToPage("./Login");
            }

            ReturnUrl = returnUrl ?? Url.Content("~/");
            return Page();
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                _logger.LogWarning("MFA Recovery: Two-factor user was null, redirecting to Login.");
                return RedirectToPage("./Login");
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            var recoveryCode = Input.RecoveryCode.Replace(" ", string.Empty);
            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID '{UserId}' logged in with a recovery code.", user.Id);

                user.LastLoginDate = DateTime.UtcNow;
                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                var roles = await _userManager.GetRolesAsync(user);

                if (roles.Contains("Super Admin")) return LocalRedirect("/Admin/Dashboard");
                if (roles.Contains("Admin")) return LocalRedirect("/Admin/Dashboard");
                if (roles.Contains("Sales Manager")) return LocalRedirect("/SalesManager/Dashboard");
                if (roles.Contains("Sales Staff")) return LocalRedirect("/SalesStaff/Dashboard");
                if (roles.Contains("Support Staff")) return LocalRedirect("/SupportStaff/Dashboard");
                if (roles.Contains("Marketing Manager") || roles.Contains("Marketing Staff")) return LocalRedirect("/MarketingStaff/Dashboard");
                if (roles.Contains("Billing Staff")) return LocalRedirect("/BillingStaff/Dashboard");
                if (roles.Contains("Customer")) return LocalRedirect("/CustomerPortal/Dashboard");

                return LocalRedirect(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User account locked out.");
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "MFA Recovery — Account Locked",
                    Description = $"Account locked after repeated recovery code failures for {user.Email}",
                    UserId = user.Id,
                    UserName = user.Email ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                });
                await _context.SaveChangesAsync();
                return RedirectToPage("./Lockout");
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID '{UserId}'.", user.Id);
                _context.AuditLogs.Add(new AuditLog
                {
                    Action = "MFA Recovery — Invalid Code",
                    Description = $"Invalid recovery code entered for {user.Email}",
                    UserId = user.Id,
                    UserName = user.Email ?? "Unknown",
                    Timestamp = DateTime.UtcNow,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                });
                await _context.SaveChangesAsync();
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return Page();
            }
        }
    }
}
