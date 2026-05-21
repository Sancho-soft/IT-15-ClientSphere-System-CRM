using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using ClientSphere.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientSphere.Areas.Identity.Pages.Account
{
    public class LoginWith2faModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<LoginWith2faModel> _logger;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;
        private readonly ClientSphere.Data.ApplicationDbContext _context;
        private readonly IDataProtector _protector;

        public LoginWith2faModel(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<LoginWith2faModel> logger,
            Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender,
            ClientSphere.Data.ApplicationDbContext context,
            IDataProtectionProvider dp)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
            _emailSender = emailSender;
            _context = context;
            _protector = dp.CreateProtector("ClientSphere.MFA.OTP.v2");
        }

        // ── Bound properties ──

        [BindProperty]
        public InputModel Input { get; set; } = new();

        [BindProperty]
        public bool RememberMe { get; set; }

        [BindProperty]
        public string ReturnUrl { get; set; } = string.Empty;

        /// <summary>
        /// Encrypted payload that carries the OTP inside the form itself.
        /// Format after decryption: code|userId|expiryUtc
        /// </summary>
        [BindProperty]
        public string EncryptedOtp { get; set; } = string.Empty;

        public string MaskedEmail { get; set; } = string.Empty;

        public string? StatusMessage { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "Please enter the verification code.")]
            [StringLength(7, ErrorMessage = "Code must be 6 digits.", MinimumLength = 6)]
            [DataType(DataType.Text)]
            [Display(Name = "Verification Code")]
            public string TwoFactorCode { get; set; } = string.Empty;

            [Display(Name = "Remember this device")]
            public bool RememberMachine { get; set; }
        }

        // ── Helpers ──

        private static string GenerateOtpCode()
        {
            return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
        }

        private string EncryptOtp(string code, string userId)
        {
            var payload = $"{code}|{userId}|{DateTime.UtcNow.AddMinutes(5):O}";
            return _protector.Protect(payload);
        }

        private (string? code, string? userId) DecryptOtp(string encrypted)
        {
            try
            {
                var decrypted = _protector.Unprotect(encrypted);
                var parts = decrypted.Split('|', 3);
                if (parts.Length != 3) return (null, null);

                if (DateTime.TryParse(parts[2], out var expiry) && DateTime.UtcNow > expiry)
                {
                    _logger.LogWarning("OTP expired.");
                    return (null, null);
                }

                return (parts[0], parts[1]);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to decrypt OTP payload.");
                return (null, null);
            }
        }

        private async Task<string> SendOtpEmail(ApplicationUser user, string otpCode)
        {
            var htmlMessage = $@"
                <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto;'>
                    <h2 style='color: #1a1a2e;'>ClientSphere Verification Code</h2>
                    <p>Hi {user.FirstName},</p>
                    <p>Your two-factor authentication code is:</p>
                    <div style='background: #f0f4ff; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                        <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1a1a2e;'>{otpCode}</span>
                    </div>
                    <p style='color: #666; font-size: 14px;'>This code expires in 5 minutes.</p>
                    <hr style='border: none; border-top: 1px solid #eee; margin: 20px 0;' />
                    <p style='color: #999; font-size: 12px;'>ClientSphere CRM — Security Team</p>
                </div>";

            await _emailSender.SendEmailAsync(user.Email!, "Your ClientSphere Verification Code", htmlMessage);
            return otpCode;
        }

        private static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email)) return "***";
            var parts = email.Split('@');
            if (parts.Length != 2) return "***";
            var local = parts[0];
            var domain = parts[1];
            if (local.Length <= 2) return local[0] + "***@" + domain;
            return local[0] + new string('*', local.Length - 2) + local[^1] + "@" + domain;
        }

        private IActionResult RedirectToDashboard(IList<string> roles, string fallbackUrl)
        {
            if (roles.Contains("Super Admin") || roles.Contains("Admin")) return LocalRedirect("/Admin/Dashboard");
            if (roles.Contains("Sales Manager")) return LocalRedirect("/SalesManager/Dashboard");
            if (roles.Contains("Sales Staff")) return LocalRedirect("/SalesStaff/Dashboard");
            if (roles.Contains("Support Staff")) return LocalRedirect("/SupportStaff/Dashboard");
            if (roles.Contains("Marketing Manager") || roles.Contains("Marketing Staff")) return LocalRedirect("/MarketingStaff/Dashboard");
            if (roles.Contains("Billing Staff")) return LocalRedirect("/BillingStaff/Dashboard");
            if (roles.Contains("Customer")) return LocalRedirect("/CustomerPortal/Dashboard");
            return LocalRedirect(fallbackUrl);
        }

        // ── GET: Show the 2FA form & send the email ──

        public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToPage("./Login");

            ReturnUrl = returnUrl ?? Url.Content("~/");
            RememberMe = rememberMe;
            MaskedEmail = MaskEmail(user.Email ?? "");

            // Generate OTP, encrypt it, put it in the hidden field
            var otpCode = GenerateOtpCode();
            EncryptedOtp = EncryptOtp(otpCode, user.Id);

            _logger.LogInformation("2FA: Generated OTP for user {UserId}", user.Id);

            try
            {
                await SendOtpEmail(user, otpCode);
                _logger.LogInformation("2FA: Email sent to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "2FA: Email send failed for user {UserId}", user.Id);
                StatusMessage = "Could not send the email. Please click Resend Code.";
            }

            return Page();
        }

        // ── POST: Verify the code (default handler, no asp-page-handler needed) ──

        public async Task<IActionResult> OnPostAsync()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToPage("./Login");

            MaskedEmail = MaskEmail(user.Email ?? "");

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("2FA: ModelState invalid");
                return Page();
            }

            // Read the encrypted OTP from the hidden field
            if (string.IsNullOrEmpty(EncryptedOtp))
            {
                _logger.LogWarning("2FA: EncryptedOtp is empty on POST");
                ModelState.AddModelError(string.Empty, "Session expired. Please click Resend Code to get a new code.");
                return Page();
            }

            var (storedCode, storedUserId) = DecryptOtp(EncryptedOtp);

            if (storedCode == null || storedUserId == null)
            {
                ModelState.AddModelError(string.Empty, "Your code has expired. Please click Resend Code.");
                return Page();
            }

            if (storedUserId != user.Id)
            {
                ModelState.AddModelError(string.Empty, "Invalid session. Please log in again.");
                return Page();
            }

            var enteredCode = Input.TwoFactorCode.Replace(" ", "").Replace("-", "");

            _logger.LogInformation("2FA: Comparing entered={Entered} vs stored={Stored} for user {UserId}",
                enteredCode, storedCode, user.Id);

            if (enteredCode == storedCode)
            {
                // ✅ Code matches — sign in the user
                await _signInManager.SignInAsync(user, isPersistent: RememberMe);
                _logger.LogInformation("2FA: User {UserId} verified and signed in.", user.Id);

                user.LastLoginDate = DateTime.UtcNow;
                user.IsActive = true;
                await _userManager.UpdateAsync(user);

                var roles = await _userManager.GetRolesAsync(user);
                return RedirectToDashboard(roles, ReturnUrl ?? "/");
            }

            // ❌ Wrong code
            _logger.LogWarning("2FA: Invalid code for user {UserId}", user.Id);

            _context.AuditLogs.Add(new AuditLog
            {
                Action = "MFA Failed — Invalid Code",
                Description = $"Invalid MFA code entered for {user.Email}",
                UserId = user.Id,
                UserName = user.Email ?? "Unknown",
                Timestamp = DateTime.UtcNow,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
            });
            await _context.SaveChangesAsync();

            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
                return RedirectToPage("./Lockout");

            ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
            return Page();
        }

        // ── POST: Resend Code handler ──

        public async Task<IActionResult> OnPostResendCodeAsync()
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null) return RedirectToPage("./Login");

            MaskedEmail = MaskEmail(user.Email ?? "");

            var otpCode = GenerateOtpCode();
            EncryptedOtp = EncryptOtp(otpCode, user.Id);

            try
            {
                await SendOtpEmail(user, otpCode);
                StatusMessage = "A new verification code has been sent to your email.";
                _logger.LogInformation("2FA: Resent OTP to user {UserId}", user.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "2FA: Resend failed for user {UserId}", user.Id);
                StatusMessage = "Failed to send the code. Please try again.";
            }

            return Page();
        }
    }
}
