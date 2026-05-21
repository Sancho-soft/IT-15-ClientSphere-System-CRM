# Implementation Tasks — Security and System Fixes

## Tasks

- [x] 1. Enable Account Lockout
  - [x] 1.1 In `Areas/Identity/Pages/Account/Login.cshtml.cs`, change `lockoutOnFailure: false` to `lockoutOnFailure: true` in the `PasswordSignInAsync` call
  - [x] 1.2 In `Program.cs`, add lockout configuration inside the `AddIdentity` options block: `options.Lockout.MaxFailedAccessAttempts = 5`, `options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15)`, `options.Lockout.AllowedForNewUsers = true`

- [x] 2. Fix IDOR in CustomerPortalController
  - [x] 2.1 In `Data/DbInitializer.cs`, update the customer seed block to set `UserId = customerUser2.Id` when creating the `Customer` record, and add a repair step to set `UserId` if it is currently null on the existing seed record
  - [x] 2.2 In `Controllers/CustomerPortalController.cs`, replace all 6 email-based customer lookups (`_context.Customers.FirstOrDefault(c => c.Email == userName)`) in `Dashboard`, `MyOrders`, `MyInvoices`, `ArchiveInvoice`, `PayWithPaymongo`, and `PaymentSuccess` with UserId-based lookups (`await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId)`)
  - [x] 2.3 Run `dotnet ef migrations add AddUserIdToCustomer` and `dotnet ef database update` to ensure the `UserId` column exists in the database

- [x] 3. Encrypt OAuth Tokens in Session
  - [x] 3.1 In `Controllers/SalesStaffController.cs`, add `IDataProtectionProvider` parameter to the constructor and create a named protector: `_tokenProtector = dataProtectionProvider.CreateProtector("GraphToken.v1")`
  - [x] 3.2 In `OAuthCallback`, wrap the token before storing: `HttpContext.Session.SetString($"GraphToken_{state}", _tokenProtector.Protect(accessToken))`
  - [x] 3.3 In `SyncToOutlook`, unwrap the token after reading: `var accessToken = _tokenProtector.Unprotect(encryptedToken)` with appropriate error handling for invalid/expired tokens

- [x] 4. Add Missing Security Headers
  - [x] 4.1 In `Program.cs`, extend the existing security headers middleware to append `Permissions-Policy` header disabling unused browser features (accelerometer, camera, geolocation, gyroscope, magnetometer, microphone, payment, usb)
  - [x] 4.2 In `Program.cs`, append `Content-Security-Policy` header allowing self, Cloudflare Turnstile (`challenges.cloudflare.com`), Cloudinary images (`res.cloudinary.com`), CDN assets, and Google Fonts
  - [x] 4.3 In `Program.cs`, append `Strict-Transport-Security` header with `max-age=31536000; includeSubDomains` inside a `!app.Environment.IsDevelopment()` guard

- [x] 5. Add File Upload Validation
  - [x] 5.1 Create `Helpers/FileUploadValidator.cs` with a static class containing `IsValidImageType(IFormFile file)` (whitelist: image/jpeg, image/png, image/gif, image/webp) and `IsWithinSizeLimit(IFormFile file)` (max 5 MB = 5 * 1024 * 1024 bytes) methods
  - [x] 5.2 In `Controllers/SupportController.cs` `Create` POST action, add validation calls before the Cloudinary upload and return the view with an error message if validation fails
  - [x] 5.3 In `Controllers/CustomerPortalController.cs` `MyProfile` POST action, add validation calls before the Cloudinary upload and redirect with an error message if validation fails
  - [x] 5.4 In `Controllers/ProductsController.cs` `Create` and `Edit` POST actions, add validation calls before the Cloudinary upload and add a `ModelState` error if validation fails

- [x] 6. Fix Audit Logging Gaps
  - [x] 6.1 In `Areas/Identity/Pages/Account/Login.cshtml.cs`, inject `ApplicationDbContext` via constructor and add an `AuditLog` entry after Turnstile verification failure recording the attempted email, failure reason, timestamp, and IP address
  - [x] 6.2 In `Login.cshtml.cs`, add an `AuditLog` entry after invalid credentials (`ModelState.AddModelError` for failed login) recording the attempted email, failure reason, timestamp, and IP address
  - [x] 6.3 In `Login.cshtml.cs`, add an `AuditLog` entry after `result.IsLockedOut` recording the attempted email, lockout reason, timestamp, and IP address
  - [x] 6.4 In `Controllers/AdminController.cs` `CreateUser` action, add an `AuditLog` entry after `result.Succeeded` recording the acting user, new user email, assigned role, timestamp, and IP address
  - [x] 6.5 In `AdminController.cs` `EditUser` action, add an `AuditLog` entry after role update succeeds recording the acting user, target user email, new role, timestamp, and IP address
  - [x] 6.6 In `AdminController.cs` `DeactivateUser` action, add an `AuditLog` entry after `result.Succeeded` recording the acting user, deactivated user email, timestamp, and IP address
  - [x] 6.7 In `AdminController.cs` `ReactivateUser` action, add an `AuditLog` entry after `result.Succeeded` recording the acting user, reactivated user email, timestamp, and IP address
  - [x] 6.8 In `AdminController.cs` `ResetPassword` action, add an `AuditLog` entry after `result.Succeeded` recording the acting user, target user email, timestamp, and IP address

- [x] 7. Fix Geolocation HTTP Client
  - [x] 7.1 In `Program.cs`, update the `IIpGeolocationService` HTTP client registration to configure a 3-second timeout: `builder.Services.AddHttpClient<IIpGeolocationService, IpGeolocationService>(client => { client.Timeout = TimeSpan.FromSeconds(3); })`
  - [x] 7.2 In `Areas/Identity/Pages/Account/Login.cshtml.cs`, add `IIpGeolocationService` parameter to the constructor
  - [x] 7.3 In `Login.cshtml.cs` `OnPostAsync`, remove the inline `using var httpClient = new HttpClient()` block and replace it with a call to `_ipGeolocationService` to retrieve the location string, preserving the existing new-location email alert and `LastLoginLocation` update logic

- [x] 8. Add Model Input Validation
  - [x] 8.1 In `Models/Opportunity.cs`, add `[Range(0, double.MaxValue, ErrorMessage = "Estimated value must be 0 or greater.")]` to `EstimatedValue` and `[Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]` to `Probability`
  - [x] 8.2 In `Models/Lead.cs`, add `[StringLength(20)]` to `Phone`, `[StringLength(100)]` to `Company`, `[StringLength(50)]` to `Source`, and `[StringLength(20)]` to `Status`
  - [x] 8.3 In `Models/SupportTicket.cs`, add `[StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]` to `Subject` and `[StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]` to `Description`

- [x] 9. Fix SupportController Authorization
  - [x] 9.1 In `Controllers/SupportController.cs`, replace the class-level `[Authorize]` attribute with `[Authorize(Roles = "Super Admin,Admin,Support Staff")]`

- [x] 10. Fix N+1 Queries and In-Memory Filtering
  - [x] 10.1 In `Controllers/CustomersController.cs` `Index` action, move the `IsActive` filter to execute before materialisation — verify whether the service/repository returns `IQueryable<Customer>` and if so apply `.Where(c => archived ? !c.IsActive : c.IsActive)` before `.ToList()`/`.ToListAsync()`; otherwise add a `GetCustomersByStatusAsync(bool isActive)` method to the service and repository
  - [x] 10.2 In `Controllers/SalesManagerController.cs` `Dashboard` action, move the `GroupBy` aggregation into the EF Core query so it translates to SQL: chain `.GroupBy(o => o.AssignedToUserId).Select(g => new { UserId = g.Key, TotalSales = g.Sum(x => x.EstimatedValue), DealsCount = g.Count() })` before `.ToListAsync()` instead of calling `.ToListAsync()` first

- [x] 11. Fix Generic Exception Catching
  - [x] 11.1 In `Controllers/CustomersController.cs` `Edit` POST action, replace `catch (Exception)` with `catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)`
  - [x] 11.2 In `Controllers/BillingController.cs` `Edit` POST action, replace `catch (Exception)` with `catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)`
  - [x] 11.3 In `Controllers/ProductsController.cs` `Edit` POST action, replace `catch (Exception)` with `catch (Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException)`

- [x] 12. Write Property-Based Tests for Bug Condition Verification
  - [x] 12.1 Write a test that verifies account lockout is triggered after 5 consecutive failed login attempts (assert `result.IsLockedOut == true` on the 6th attempt)
  - [x] 12.2 Write a test that verifies `CustomerPortalController` actions return 404 when the authenticated user's `UserId` does not match any `Customer.UserId` in the database (IDOR prevention)
  - [x] 12.3 Write a test that verifies `FileUploadValidator.IsValidImageType` returns false for non-image MIME types (e.g., `application/pdf`, `text/html`) and true for whitelisted types
  - [x] 12.4 Write a test that verifies `FileUploadValidator.IsWithinSizeLimit` returns false for files exceeding 5 MB and true for files at or below the limit
  - [x] 12.5 Write a test that verifies `Opportunity` model validation rejects negative `EstimatedValue` and `Probability` values outside 0–100
