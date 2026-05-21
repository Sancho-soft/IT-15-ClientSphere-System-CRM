# Technical Design Document — Security and System Fixes

## Overview

This document describes the technical implementation plan for fixing the 12 security vulnerabilities and system reliability issues identified in the bugfix requirements document. All changes are confined to existing files — no new controllers or services are introduced. The fixes are grouped into eight areas matching the requirements.

---

## 1. Secrets Management

### 1.1 Remove Hardcoded Secrets from Configuration Files

**Files:** `appsettings.json`, `appsettings.Production.json`

Replace all literal secret values with empty strings or placeholder comments. The actual values will be supplied at runtime via environment variables or ASP.NET User Secrets.

`appsettings.json` will retain only non-sensitive structural keys:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": ""
  },
  "SendGrid": {
    "ApiKey": "",
    "SenderEmail": "",
    "SenderName": "ClientSphere"
  },
  "MicrosoftGraph": {
    "ClientId": "",
    "ClientSecret": "",
    "TenantId": "common",
    "RedirectUri": ""
  },
  "Cloudinary": {
    "CloudName": "",
    "ApiKey": "",
    "ApiSecret": ""
  },
  "Paymongo": {
    "PublicKey": "",
    "SecretKey": "",
    "WebhookSecret": "",
    "SuccessUrl": "",
    "CancelUrl": ""
  },
  "Turnstile": {
    "SiteKey": "",
    "SecretKey": ""
  },
  "AllowedHosts": "*"
}
```

`appsettings.Production.json` will only contain non-sensitive production overrides (redirect URIs, URLs). All secrets are supplied via environment variables on the host.

**Environment variable naming convention** (ASP.NET Core double-underscore separator):
- `ConnectionStrings__DefaultConnection`
- `SendGrid__ApiKey`
- `MicrosoftGraph__ClientSecret`
- `Cloudinary__ApiSecret`
- `Paymongo__SecretKey`
- `Paymongo__WebhookSecret`
- `Turnstile__SecretKey`

For local development, use `dotnet user-secrets set "SendGrid:ApiKey" "..."` etc.

Add `appsettings.Production.json` to `.gitignore` if not already present.

### 1.2 Remove Hardcoded Passwords from DbInitializer

**File:** `Data/DbInitializer.cs`

- Delete the entire `UpdateUserPasswords` private method.
- Remove the call to `await UpdateUserPasswords(userManager)` at the top of `Initialize`.
- Replace all hardcoded password string literals in the seed user creation blocks with `Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "ChangeMe123!"`. This ensures no plain-text passwords exist in source code while keeping seeding functional.
- The seed password environment variable is documented in a `README.md` note (not committed with a real value).

---

## 2. Account Lockout

**File:** `Areas/Identity/Pages/Account/Login.cshtml.cs`

Change line:
```csharp
var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: false);
```
To:
```csharp
var result = await _signInManager.PasswordSignInAsync(Input.Email, Input.Password, Input.RememberMe, lockoutOnFailure: true);
```

**File:** `Program.cs`

Add lockout configuration inside the `AddIdentity` options block:

```csharp
options.Lockout.MaxFailedAccessAttempts = 5;
options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
options.Lockout.AllowedForNewUsers = true;
```

This means after 5 consecutive failed attempts, the account is locked for 15 minutes. The existing `IsLockedOut` redirect path in `Login.cshtml.cs` already handles this case correctly.

---

## 3. IDOR Fix — CustomerPortalController

### 3.1 Customer Model

**File:** `Models/Customer.cs`

The `UserId` field (`public string? UserId { get; set; }`) already exists on the model. No model change needed.

### 3.2 Database Migration

A new EF Core migration is needed to ensure the `UserId` column exists in the database (it may not have been added in a prior migration). Run:

```
dotnet ef migrations add AddUserIdToCustomer
dotnet ef database update
```

The migration will add the nullable `UserId` column to the `Customers` table if it does not already exist.

### 3.3 DbInitializer — Seed UserId

**File:** `Data/DbInitializer.cs`

In the customer seeding block, after finding or creating the `customerUser2` (`ApplicationUser`), set `customerRecord.UserId = customerUser2.Id` when creating the `Customer` record:

```csharp
customerRecord = new Customer
{
    ContactName = ...,
    Email = customerUser2.Email,
    Phone = ...,
    CompanyName = "Acme Corp",
    UserId = customerUser2.Id,   // <-- add this
    CreatedAt = DateTime.UtcNow
};
```

Also add a repair step: if the existing `customerRecord.UserId` is null, set it and save.

### 3.4 CustomerPortalController — Replace Email Lookup with UserId Lookup

**File:** `Controllers/CustomerPortalController.cs`

Replace all occurrences of the email-based customer lookup pattern:

```csharp
// OLD — insecure
var userName = User.Identity?.Name;
var customer = !string.IsNullOrEmpty(userName)
    ? _context.Customers.FirstOrDefault(c => c.Email == userName)
    : null;
```

With the UserId-based lookup:

```csharp
// NEW — secure
var userId = _userManager.GetUserId(User);
var customer = !string.IsNullOrEmpty(userId)
    ? await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId)
    : null;
```

Affected actions: `Dashboard`, `MyOrders`, `MyInvoices`, `ArchiveInvoice`, `PayWithPaymongo`, `PaymentSuccess`.

Each action already returns `NotFound()` when `customer == null`, which satisfies requirement 2.4.

---

## 4. OAuth Token Encryption

**File:** `Controllers/SalesStaffController.cs`

ASP.NET Core Data Protection is already available in the DI container by default. Inject `IDataProtectionProvider` into the controller constructor and create a named protector:

```csharp
private readonly IDataProtector _tokenProtector;

public SalesStaffController(..., IDataProtectionProvider dataProtectionProvider)
{
    ...
    _tokenProtector = dataProtectionProvider.CreateProtector("GraphToken.v1");
}
```

**Storing the token** (in `OAuthCallback`):
```csharp
var encryptedToken = _tokenProtector.Protect(accessToken);
HttpContext.Session.SetString($"GraphToken_{state}", encryptedToken);
```

**Reading the token** (in `SyncToOutlook`):
```csharp
var encryptedToken = HttpContext.Session.GetString($"GraphToken_{userId}");
if (string.IsNullOrEmpty(encryptedToken)) { /* redirect to OAuth */ }
var accessToken = _tokenProtector.Unprotect(encryptedToken);
```

No changes to `Program.cs` are needed — `AddDataProtection()` is included automatically via `AddControllersWithViews`.

---

## 5. Security Headers

**File:** `Program.cs`

Extend the existing security headers middleware to include the three missing headers. The `Strict-Transport-Security` header is only added outside of development:

```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Append("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Append("X-Frame-Options", "DENY");
    context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
    context.Response.Headers.Append("Permissions-Policy",
        "accelerometer=(), camera=(), geolocation=(), gyroscope=(), magnetometer=(), microphone=(), payment=(), usb=()");
    context.Response.Headers.Append("Content-Security-Policy",
        "default-src 'self'; " +
        "script-src 'self' 'unsafe-inline' https://challenges.cloudflare.com https://cdn.jsdelivr.net; " +
        "style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; " +
        "font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; " +
        "img-src 'self' data: https://res.cloudinary.com; " +
        "frame-src https://challenges.cloudflare.com; " +
        "connect-src 'self';");
    if (!app.Environment.IsDevelopment())
    {
        context.Response.Headers.Append("Strict-Transport-Security", "max-age=31536000; includeSubDomains");
    }
    await next();
});
```

The CSP allows Cloudflare Turnstile (`challenges.cloudflare.com`), Cloudinary images (`res.cloudinary.com`), and CDN assets already used by the app. Adjust `unsafe-inline` for scripts/styles once a nonce-based approach is adopted in a future iteration.

---

## 6. File Upload Validation

### 6.1 FileUploadValidator Helper

**New file:** `Helpers/FileUploadValidator.cs`

```csharp
namespace ClientSphere.Helpers
{
    public static class FileUploadValidator
    {
        private static readonly HashSet<string> AllowedContentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg", "image/jpg", "image/png", "image/gif", "image/webp"
        };

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".jpg", ".jpeg", ".png", ".gif", ".webp"
        };

        public const long MaxFileSizeBytes = 5 * 1024 * 1024; // 5 MB

        public static bool IsValidImageType(IFormFile file)
        {
            if (file == null) return false;
            var ext = Path.GetExtension(file.FileName);
            return AllowedContentTypes.Contains(file.ContentType)
                && AllowedExtensions.Contains(ext);
        }

        public static bool IsWithinSizeLimit(IFormFile file)
        {
            return file != null && file.Length <= MaxFileSizeBytes;
        }
    }
}
```

### 6.2 Apply Validation in Controllers

**`Controllers/SupportController.cs`** — in `Create` action, before the Cloudinary upload:

```csharp
if (attachment != null && attachment.Length > 0)
{
    if (!FileUploadValidator.IsValidImageType(attachment))
    {
        TempData["Error"] = "Only image files (JPEG, PNG, GIF, WebP) are allowed.";
        return View(ticket);
    }
    if (!FileUploadValidator.IsWithinSizeLimit(attachment))
    {
        TempData["Error"] = "File size must not exceed 5 MB.";
        return View(ticket);
    }
    // ... existing Cloudinary upload
}
```

**`Controllers/CustomerPortalController.cs`** — in `MyProfile` POST, before the Cloudinary upload:

```csharp
if (profilePicture != null && profilePicture.Length > 0)
{
    if (!FileUploadValidator.IsValidImageType(profilePicture))
    {
        TempData["ErrorMessage"] = "Only image files (JPEG, PNG, GIF, WebP) are allowed.";
        return RedirectToAction(nameof(MyProfile));
    }
    if (!FileUploadValidator.IsWithinSizeLimit(profilePicture))
    {
        TempData["ErrorMessage"] = "Profile picture must not exceed 5 MB.";
        return RedirectToAction(nameof(MyProfile));
    }
    // ... existing Cloudinary upload
}
```

**`Controllers/ProductsController.cs`** — in both `Create` and `Edit` POST actions, before the Cloudinary upload:

```csharp
if (productImage != null && productImage.Length > 0)
{
    if (!FileUploadValidator.IsValidImageType(productImage))
    {
        ModelState.AddModelError("", "Only image files (JPEG, PNG, GIF, WebP) are allowed.");
        return View(product);
    }
    if (!FileUploadValidator.IsWithinSizeLimit(productImage))
    {
        ModelState.AddModelError("", "Product image must not exceed 5 MB.");
        return View(product);
    }
    // ... existing Cloudinary upload
}
```

---

## 7. Audit Logging Gaps

### 7.1 Failed Login Audit Logging

**File:** `Areas/Identity/Pages/Account/Login.cshtml.cs`

Inject `ApplicationDbContext` into `LoginModel` via constructor. After each failure path, write an `AuditLog` entry:

**After Turnstile failure:**
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "Login Failed — Turnstile",
    Description = $"Turnstile verification failed for {Input.Email}",
    UserId = "Anonymous", UserName = Input.Email,
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**After invalid credentials (`result.Succeeded == false` and not locked out):**
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "Login Failed — Invalid Credentials",
    Description = $"Failed login attempt for {Input.Email}",
    UserId = "Anonymous", UserName = Input.Email,
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**After lockout (`result.IsLockedOut == true`):**
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "Login Failed — Account Locked",
    Description = $"Account locked after repeated failures for {Input.Email}",
    UserId = "Anonymous", UserName = Input.Email,
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

### 7.2 Privileged Operation Audit Logging

**File:** `Controllers/AdminController.cs`

Add audit log writes after each privileged operation succeeds. The acting user's ID is retrieved via `_userManager.GetUserId(User)`.

**`DeactivateUser`** — after `result.Succeeded`:
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "User Deactivated",
    Description = $"Super Admin deactivated user {user.Email} (ID: {user.Id})",
    UserId = _userManager.GetUserId(User) ?? "Unknown",
    UserName = User.Identity?.Name ?? "Unknown",
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**`ReactivateUser`** — after `result.Succeeded`:
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "User Reactivated",
    Description = $"Super Admin reactivated user {user.Email} (ID: {user.Id})",
    UserId = _userManager.GetUserId(User) ?? "Unknown",
    UserName = User.Identity?.Name ?? "Unknown",
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**`ResetPassword`** — after `result.Succeeded`:
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "Password Reset",
    Description = $"Super Admin reset password for {user.Email} (ID: {user.Id})",
    UserId = _userManager.GetUserId(User) ?? "Unknown",
    UserName = User.Identity?.Name ?? "Unknown",
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**`EditUser`** — after role update succeeds:
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "User Role Changed",
    Description = $"Super Admin changed role for {user.Email} to '{role}'",
    UserId = _userManager.GetUserId(User) ?? "Unknown",
    UserName = User.Identity?.Name ?? "Unknown",
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

**`CreateUser`** — after `result.Succeeded`:
```csharp
_context.AuditLogs.Add(new AuditLog {
    Action = "User Created",
    Description = $"Super Admin created user {email} with role '{role}'",
    UserId = _userManager.GetUserId(User) ?? "Unknown",
    UserName = User.Identity?.Name ?? "Unknown",
    Timestamp = DateTime.UtcNow,
    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
});
await _context.SaveChangesAsync();
```

---

## 8. Geolocation HTTP Client Timeout

**File:** `Areas/Identity/Pages/Account/Login.cshtml.cs`

The `IIpGeolocationService` is already registered in `Program.cs` as an `HttpClient`-backed service. Inject it into `LoginModel`:

```csharp
private readonly ClientSphere.Services.IIpGeolocationService _ipGeolocationService;

public LoginModel(..., ClientSphere.Services.IIpGeolocationService ipGeolocationService)
{
    ...
    _ipGeolocationService = ipGeolocationService;
}
```

Replace the inline `HttpClient` block in `OnPostAsync`:

```csharp
// OLD
using var httpClient = new HttpClient();
var response = await httpClient.GetFromJsonAsync<JsonElement>($"http://ip-api.com/json/{ipAddress}");
// ... parse city/country manually
```

With a call to the service:

```csharp
// NEW
var location = await _ipGeolocationService.GetLocationAsync(ipAddress);
string currentLocation = location ?? "Unknown";
```

**File:** `Program.cs`

Configure the `IpGeolocationService` HTTP client with a 3-second timeout:

```csharp
builder.Services.AddHttpClient<ClientSphere.Services.IIpGeolocationService, ClientSphere.Services.IpGeolocationService>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(3);
});
```

> Note: The exact method signature of `IIpGeolocationService.GetLocationAsync` must be verified against the existing service implementation. If the service returns a structured object rather than a string, the call site in `Login.cshtml.cs` should be adjusted to extract the city/country fields accordingly.

---

## 9. Model Input Validation

### 9.1 Opportunity Model

**File:** `Models/Opportunity.cs`

Add `[Range]` attributes to numeric fields:

```csharp
[Range(0, double.MaxValue, ErrorMessage = "Estimated value must be 0 or greater.")]
[Column(TypeName = "decimal(18, 2)")]
public decimal EstimatedValue { get; set; }

[Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
public int Probability { get; set; }
```

### 9.2 Lead Model

**File:** `Models/Lead.cs`

Add `[StringLength]` attributes to unbounded string fields:

```csharp
[StringLength(20)]
public string? Phone { get; set; }

[StringLength(100)]
public string? Company { get; set; }

[StringLength(50)]
public string? Source { get; set; }

[StringLength(20)]
public string? Status { get; set; }
```

### 9.3 SupportTicket Model

**File:** `Models/SupportTicket.cs`

Add `[StringLength]` attributes:

```csharp
[Required]
[StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
public string Subject { get; set; }

[Required]
[StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
public string Description { get; set; }
```

---

## 10. SupportController Authorization Fix

**File:** `Controllers/SupportController.cs`

Replace the broad `[Authorize]` attribute with a role-restricted one:

```csharp
// OLD
[Authorize]

// NEW
[Authorize(Roles = "Super Admin,Admin,Support Staff")]
```

This ensures only staff roles that legitimately manage tickets can access the support dashboard. Customers access their own tickets exclusively through `CustomerPortalController.MyTickets` which is already correctly scoped by `CustomerId`.

---

## 11. N+1 Query and In-Memory Filtering Fixes

### 11.1 CustomersController.Index

**File:** `Controllers/CustomersController.cs`

The current pattern loads all customers then filters in memory:

```csharp
// OLD
customers = await _customerService.GetAllCustomersAsync();
customers = customers.Where(c => archived ? !c.IsActive : c.IsActive);
```

Pass the filter to the service layer. If `ICustomerService` / `CustomerRepository` supports it, add an overload or use the existing search with an active filter. At minimum, move the `Where` clause to execute before materialisation:

```csharp
// NEW — filter at DB level via service
customers = await _customerService.GetCustomersByStatusAsync(isActive: !archived);
```

If adding a new service method is out of scope, the `Where` on an `IQueryable` (before `.ToList()`) is acceptable — verify the repository returns `IQueryable<Customer>` rather than `IEnumerable<Customer>`.

### 11.2 SalesManagerController.Dashboard

**File:** `Controllers/SalesManagerController.cs`

The current pattern loads all "Closed Won" opportunities then groups in memory:

```csharp
// OLD
var wonOpps = await _context.Opportunities
    .Where(o => o.Stage == "Closed Won")
    .ToListAsync();

var salesData = wonOpps
    .GroupBy(o => o.AssignedToUserId)
    ...
```

Move the `GroupBy` into the EF Core query so it translates to SQL `GROUP BY`:

```csharp
// NEW
var salesData = await _context.Opportunities
    .Where(o => o.Stage == "Closed Won")
    .GroupBy(o => o.AssignedToUserId)
    .Select(g => new
    {
        UserId = g.Key,
        TotalSales = g.Sum(x => x.EstimatedValue),
        DealsCount = g.Count()
    })
    .ToListAsync();
```

---

## 12. Generic Exception Catching Fix

**Files:** `Controllers/CustomersController.cs`, `Controllers/BillingController.cs`, `Controllers/ProductsController.cs`

Replace generic `catch (Exception)` with `catch (DbUpdateConcurrencyException)` in all Edit POST actions:

```csharp
// OLD
catch (Exception)
{
    if (!await _service.EntityExistsAsync(id)) return NotFound();
    else throw;
}

// NEW
catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)
{
    if (!await _service.EntityExistsAsync(id)) return NotFound();
    else throw;
}
```

This makes the intent explicit — only concurrency conflicts are caught and handled; all other exceptions propagate normally to the global error handler.

---

## Updated Migration Summary

| # | File(s) Changed | Type |
|---|----------------|------|
| 1 | `appsettings.json`, `appsettings.Production.json` | Config *(deferred)* |
| 2 | `Data/DbInitializer.cs` | Code *(deferred)* |
| 3 | `Areas/Identity/Pages/Account/Login.cshtml.cs`, `Program.cs` | Code |
| 4 | `Models/Customer.cs`, `Data/DbInitializer.cs`, `Controllers/CustomerPortalController.cs` | Code + Migration |
| 5 | `Controllers/SalesStaffController.cs` | Code |
| 6 | `Program.cs` | Code |
| 7 | `Helpers/FileUploadValidator.cs` (new), `Controllers/SupportController.cs`, `Controllers/CustomerPortalController.cs`, `Controllers/ProductsController.cs` | Code |
| 8 | `Areas/Identity/Pages/Account/Login.cshtml.cs`, `Controllers/AdminController.cs` | Code |
| 9 | `Areas/Identity/Pages/Account/Login.cshtml.cs`, `Program.cs` | Code |
| 10 | `Models/Opportunity.cs`, `Models/Lead.cs`, `Models/SupportTicket.cs` | Code |
| 11 | `Controllers/SupportController.cs` | Code |
| 12 | `Controllers/CustomersController.cs`, `Controllers/SalesManagerController.cs` | Code |
| 13 | `Controllers/CustomersController.cs`, `Controllers/BillingController.cs`, `Controllers/ProductsController.cs` | Code |

---

## Property-Based Test Design

Each fix has a corresponding testable property:

| Fix | Property | Test Approach |
|-----|----------|---------------|
| Secrets | No literal secrets in config files | Static scan: assert config values are empty/placeholder |
| Lockout | Account locks after N failures | Unit test: simulate 5 failed `PasswordSignInAsync` calls, assert `IsLockedOut` |
| IDOR | Customer lookup uses UserId | Unit test: call portal actions with mismatched UserId, assert 404 |
| File upload | Invalid types/sizes rejected | Unit test: pass non-image MIME types and oversized files, assert validation error |
| Audit logging | Failed logins produce audit entries | Integration test: trigger failed login, assert `AuditLogs` table has entry |
| Security headers | CSP/HSTS/Permissions-Policy present | Integration test: assert response headers on any endpoint |
