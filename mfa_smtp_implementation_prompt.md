# System Prompt: Implement Stateless MFA and Gmail SMTP Service

Use the following detailed requirements, architectural specifications, and implementation templates to implement a secure, stateless Multi-Factor Authentication (MFA) flow and a Gmail SMTP-based email sender service in an ASP.NET Core application with ASP.NET Core Identity.

---

## 1. SMTP Email Sender Service

### Objective
Create an email sender service that implements `Microsoft.AspNetCore.Identity.UI.Services.IEmailSender` using the standard `System.Net.Mail` client. It must connect to Gmail's SMTP servers and load credentials securely from configuration.

### Configuration (`appsettings.json`)
```json
{
  "GmailSmtp": {
    "SenderEmail": "your-gmail-account@gmail.com",
    "SenderName": "Your Application Name",
    "AppPassword": "your-16-character-gmail-app-password"
  }
}
```
*Note: The `AppPassword` must be a Google App Password generated in the security settings of the sender's Gmail account (with 2FA enabled).*

### Implementation (`GmailSmtpEmailService.cs`)
```csharp
using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

public class GmailSmtpEmailService : IEmailSender
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GmailSmtpEmailService> _logger;
    private readonly string _senderEmail;
    private readonly string _senderName;
    private readonly string _appPassword;

    public GmailSmtpEmailService(IConfiguration configuration, ILogger<GmailSmtpEmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        _senderEmail = _configuration["GmailSmtp:SenderEmail"] ?? "";
        _senderName = _configuration["GmailSmtp:SenderName"] ?? "Application";
        _appPassword = _configuration["GmailSmtp:AppPassword"] ?? "";
    }

    public async Task SendEmailAsync(string email, string subject, string htmlMessage)
    {
        using var message = new MailMessage();
        message.From = new MailAddress(_senderEmail, _senderName);
        message.To.Add(new MailAddress(email));
        message.Subject = subject;
        message.Body = htmlMessage;
        message.IsBodyHtml = true;

        using var client = new SmtpClient("smtp.gmail.com", 587)
        {
            Credentials = new NetworkCredential(_senderEmail, _appPassword),
            EnableSsl = true
        };

        await client.SendMailAsync(message);
        _logger.LogInformation("Email sent successfully to {Email}", email);
    }
}
```

### Registration (`Program.cs`)
Ensure the service is registered in the dependency injection container:
```csharp
builder.Services.AddTransient<IEmailSender, GmailSmtpEmailService>();
```

---

## 2. Stateless MFA OTP Authentication Flow

### Problem Statement
Standard ASP.NET Core Identity 2FA often relies on session state or transient database records to store the active OTP code during the login flow. In load-balanced, multi-server, or serverless environments, this can cause authentication failures if the user hits a different server on POST, or if the server restarts.

### Solution (Stateless Cryptographic Token)
1. **Generation**: When the user triggers 2FA, generate a 6-digit OTP code.
2. **Payload Structure**: Assemble a plain text payload: `Code|UserId|ExpiryUtcDateTime`.
3. **Encryption**: Encrypt this payload using ASP.NET Core **Data Protection** (`IDataProtector`). 
4. **Client-Side Storage**: Send the encrypted payload to the browser inside a hidden form field (`EncryptedOtp`).
5. **OTP Delivery**: Send the raw 6-digit OTP to the user's registered email via Gmail SMTP.
6. **Validation**: When the user submits the form, decrypt the `EncryptedOtp` hidden field value. Validate that the payload is successfully decrypted, has not expired, belongs to the user currently attempting 2FA, and that the entered code matches the decrypted OTP.

---

## 3. Backend Page Model Implementation (`LoginWith2fa.cshtml.cs`)

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

[AllowAnonymous]
public class LoginWith2faModel : PageModel
{
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly ILogger<LoginWith2faModel> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IDataProtector _protector;

    public LoginWith2faModel(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        ILogger<LoginWith2faModel> logger,
        IEmailSender emailSender,
        IDataProtectionProvider dp)
    {
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _emailSender = emailSender;
        // Purpose string binds the encryption key to this specific operation version
        _protector = dp.CreateProtector("MFA.Stateless.OTP.v1");
    }

    [BindProperty]
    public InputModel Input { get; set; } = new();

    [BindProperty]
    public bool RememberMe { get; set; }

    [BindProperty]
    public string ReturnUrl { get; set; } = string.Empty;

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

    private static string GenerateOtpCode()
    {
        // Generates a cryptographically secure 6-digit random number
        return RandomNumberGenerator.GetInt32(100000, 999999).ToString();
    }

    private string EncryptOtp(string code, string userId)
    {
        // Sets code expiration window (5 minutes)
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

            // Validate Expiration
            if (DateTime.TryParse(parts[2], out var expiry) && DateTime.UtcNow > expiry)
            {
                _logger.LogWarning("MFA: Decrypted OTP has expired.");
                return (null, null);
            }

            return (parts[0], parts[1]);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "MFA: Failed to decrypt OTP payload.");
            return (null, null);
        }
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

    private async Task SendOtpEmailAsync(IdentityUser user, string otpCode)
    {
        var htmlMessage = $@"
            <div style='font-family: Arial, sans-serif; max-width: 480px; margin: 0 auto; border: 1px solid #eee; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #1a1a2e; text-align: center;'>Verification Code</h2>
                <p>Hi,</p>
                <p>To complete your sign-in, please enter the following 2FA verification code:</p>
                <div style='background: #f0f4ff; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0;'>
                    <span style='font-size: 32px; font-weight: bold; letter-spacing: 8px; color: #1a1a2e;'>{otpCode}</span>
                </div>
                <p style='color: #666; font-size: 14px;'>This code will expire in 5 minutes.</p>
            </div>";

        await _emailSender.SendEmailAsync(user.Email!, "Your 2FA Verification Code", htmlMessage);
    }

    public async Task<IActionResult> OnGetAsync(bool rememberMe, string? returnUrl = null)
    {
        // Identity checks if there is a pending 2FA user in the context
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        ReturnUrl = returnUrl ?? Url.Content("~/");
        RememberMe = rememberMe;
        MaskedEmail = MaskEmail(user.Email ?? "");

        // Generate, encrypt, and bind OTP
        var otpCode = GenerateOtpCode();
        EncryptedOtp = EncryptOtp(otpCode, user.Id);

        try
        {
            await SendOtpEmailAsync(user, otpCode);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA: Failed to send email to user {UserId}", user.Id);
            StatusMessage = "Failed to send verification code. Please click Resend Code.";
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        MaskedEmail = MaskEmail(user.Email ?? "");

        if (!ModelState.IsValid) return Page();

        if (string.IsNullOrEmpty(EncryptedOtp))
        {
            ModelState.AddModelError(string.Empty, "Verification context lost. Please request a new code.");
            return Page();
        }

        var (storedCode, storedUserId) = DecryptOtp(EncryptedOtp);

        if (storedCode == null || storedUserId == null)
        {
            ModelState.AddModelError(string.Empty, "Your verification code has expired. Please click Resend Code.");
            return Page();
        }

        if (storedUserId != user.Id)
        {
            ModelState.AddModelError(string.Empty, "Invalid login session. Please restart login.");
            return Page();
        }

        var enteredCode = Input.TwoFactorCode.Replace(" ", "").Replace("-", "");

        if (enteredCode == storedCode)
        {
            // Reset access failure count on successful verification
            await _userManager.ResetAccessFailedCountAsync(user);

            // Signs the user in using Identity's Two-Factor flow
            await _signInManager.SignInAsync(user, isPersistent: RememberMe);
            _logger.LogInformation("User {UserId} logged in with 2FA.", user.Id);

            // Remember device if checked
            if (Input.RememberMachine)
            {
                await _signInManager.RememberTwoFactorClientAsync(user);
            }

            return LocalRedirect(ReturnUrl ?? "/");
        }

        // Invalid code: track failure & handle potential lockout
        _logger.LogWarning("MFA: Invalid code entered for user {UserId}", user.Id);
        await _userManager.AccessFailedAsync(user);

        if (await _userManager.IsLockedOutAsync(user))
        {
            return RedirectToPage("./Lockout");
        }

        ModelState.AddModelError(string.Empty, "Invalid verification code. Please try again.");
        return Page();
    }

    public async Task<IActionResult> OnPostResendCodeAsync()
    {
        var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
        if (user == null) return RedirectToPage("./Login");

        MaskedEmail = MaskEmail(user.Email ?? "");

        var otpCode = GenerateOtpCode();
        EncryptedOtp = EncryptOtp(otpCode, user.Id);

        try
        {
            await SendOtpEmailAsync(user, otpCode);
            StatusMessage = "A new verification code has been sent to your email.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MFA: Resend failed for user {UserId}", user.Id);
            StatusMessage = "Failed to send the verification code. Please try again.";
        }

        return Page();
    }
}
```

---

## 4. Frontend View Implementation (`LoginWith2fa.cshtml`)

```html
@page
@model LoginWith2faModel
@{
    ViewData["Title"] = "Two-Factor Verification";
}

<div class="container py-5">
    <div class="row justify-content-center">
        <div class="col-md-5">
            <div class="card shadow border-0 rounded-3">
                <div class="card-body p-4">
                    <h2 class="text-center fw-bold">Verify Your Identity</h2>
                    <p class="text-center text-muted">A verification code was sent to <strong>@Model.MaskedEmail</strong></p>

                    @if (!string.IsNullOrEmpty(Model.StatusMessage))
                    {
                        <div class="alert alert-info" role="alert">
                            @Model.StatusMessage
                        </div>
                    }

                    <form method="post">
                        <div asp-validation-summary="ModelOnly" class="text-danger mb-3" role="alert"></div>

                        <!-- Hidden fields to carry state statelessly -->
                        <input type="hidden" asp-for="RememberMe" />
                        <input type="hidden" asp-for="ReturnUrl" />
                        <input type="hidden" asp-for="EncryptedOtp" />

                        <div class="mb-3 text-center">
                            <label asp-for="Input.TwoFactorCode" class="form-label fw-bold">Verification Code</label>
                            <input asp-for="Input.TwoFactorCode" class="form-control text-center fw-bold"
                                   autocomplete="off" placeholder="000000"
                                   style="letter-spacing: 6px; font-size: 1.5rem;"
                                   maxlength="7" inputmode="numeric" autofocus />
                            <span asp-validation-for="Input.TwoFactorCode" class="text-danger"></span>
                        </div>

                        <div class="form-check mb-3">
                            <input class="form-check-input" asp-for="Input.RememberMachine" />
                            <label class="form-check-label" asp-for="Input.RememberMachine">
                                Remember this device
                            </label>
                        </div>

                        <div class="d-grid mb-3">
                            <button type="submit" class="btn btn-primary btn-lg">Verify Code</button>
                        </div>
                    </form>

                    <!-- Resend code triggers POST on specific page handler -->
                    <div class="text-center">
                        <form method="post" asp-page-handler="ResendCode" class="d-inline">
                            <input type="hidden" asp-for="RememberMe" />
                            <input type="hidden" asp-for="ReturnUrl" />
                            <button type="submit" class="btn btn-link text-decoration-none">Resend Code</button>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    </div>
</div>

@section Scripts {
    <partial name="_ValidationScriptsPartial" />
}
```
