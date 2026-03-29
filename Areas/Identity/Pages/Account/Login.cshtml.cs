using System.ComponentModel.DataAnnotations;
using ClientSphere.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientSphere.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly ClientSphere.Services.ITurnstileService _turnstileService;
        private readonly Microsoft.AspNetCore.Identity.UI.Services.IEmailSender _emailSender;

        public LoginModel(
            SignInManager<ApplicationUser> signInManager, 
            ILogger<LoginModel> logger,
            ClientSphere.Services.ITurnstileService turnstileService,
            Microsoft.AspNetCore.Identity.UI.Services.IEmailSender emailSender)
        {
            _signInManager = signInManager;
            _logger = logger;
            _turnstileService = turnstileService;
            _emailSender = emailSender;
        }

        [BindProperty]
        public InputModel Input { get; set; } = new();

        public IList<AuthenticationScheme> ExternalLogins { get; set; } = new List<AuthenticationScheme>();

        public string ReturnUrl { get; set; } = string.Empty;

        [TempData]
        public string ErrorMessage { get; set; } = string.Empty;

        public class InputModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; } = string.Empty;

            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
            [BindProperty(Name = "cf-turnstile-response")]
            public string? TurnstileToken { get; set; }
        }

        public async Task OnGetAsync(string? returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            if (ModelState.IsValid)
            {
                var isHuman = await _turnstileService.VerifyTokenAsync(Input.TurnstileToken);
                if (!isHuman)
                {
                    ModelState.AddModelError(string.Empty, "Cloudflare Turnstile verification failed. Please try again.");
                    return Page();
                }

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User logged in.");
                    
                    var user = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                    if (user != null)
                    {
                        // Geolocation Login Protection
                        string ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                        if (!string.IsNullOrEmpty(ipAddress) && ipAddress != "::1" && ipAddress != "127.0.0.1")
                        {
                            try
                            {
                                using var httpClient = new HttpClient();
                                var response = await httpClient.GetFromJsonAsync<System.Text.Json.JsonElement>($"http://ip-api.com/json/{ipAddress}");
                                if (response.GetProperty("status").GetString() == "success")
                                {
                                    string city = response.GetProperty("city").GetString() ?? "Unknown";
                                    string country = response.GetProperty("country").GetString() ?? "Unknown";
                                    string currentLocation = $"{city}, {country}";

                                    if (!string.IsNullOrEmpty(user.LastLoginLocation) && user.LastLoginLocation != currentLocation)
                                    {
                                        _logger.LogWarning($"User {user.Email} logged in from a new location: {currentLocation}. Previous was {user.LastLoginLocation}");
                                        string subject = "Security Alert: New Login Location Detected";
                                        string message = $@"
                                            <h3>Security Alert</h3>
                                            <p>Hi {user.FirstName},</p>
                                            <p>We detected a new login to your ClientSphere account from an unfamiliar location.</p>
                                            <p><strong>Device IP:</strong> {ipAddress}<br/>
                                            <strong>Location:</strong> {currentLocation}<br/>
                                            <strong>Time:</strong> {DateTime.UtcNow.ToString("O")} UTC</p>
                                            <p>If this was you, you can safely ignore this email. If you did not authorize this login, please change your password immediately and contact support.</p>
                                        ";
                                        await _emailSender.SendEmailAsync(user.Email, subject, message);
                                    }

                                    user.LastLoginLocation = currentLocation;
                                }
                                user.LastLoginIp = ipAddress;
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to resolve IP location via IP-API.");
                            }
                        }

                        // Update LastLoginDate for activity tracking
                        user.LastLoginDate = DateTime.UtcNow;
                        user.IsActive = true;
                        await _signInManager.UserManager.UpdateAsync(user);

                        var roles = await _signInManager.UserManager.GetRolesAsync(user);
                        
                        // Super Admin has highest priority
                        if (roles.Contains("Super Admin")) return LocalRedirect("/Admin/Dashboard");
                        if (roles.Contains("Admin")) return LocalRedirect("/Admin/Dashboard");
                        if (roles.Contains("Sales Manager")) return LocalRedirect("/SalesManager/Dashboard");
                        if (roles.Contains("Sales Staff")) return LocalRedirect("/SalesStaff/Dashboard");
                        if (roles.Contains("Support Staff")) return LocalRedirect("/SupportStaff/Dashboard");
                        if (roles.Contains("Marketing Manager") || roles.Contains("Marketing Staff")) return LocalRedirect("/MarketingStaff/Dashboard"); // Assuming shared dashboard for now or separate?
                        if (roles.Contains("Billing Staff")) return LocalRedirect("/BillingStaff/Dashboard");
                        if (roles.Contains("Customer")) return LocalRedirect("/CustomerPortal/Dashboard");
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Page();
                }
            }

            // If we got this far, something failed, redisplay form
            return Page();
        }
    }
}
