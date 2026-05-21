# Bugfix Requirements Document

## Introduction

ClientSphere contains a cluster of security vulnerabilities and system reliability issues discovered during a comprehensive codebase audit. The issues span credential exposure, authentication weaknesses, insecure data access patterns, missing security headers, unvalidated file uploads, and insufficient audit coverage. Left unaddressed, these defects expose the application to credential theft, brute-force attacks, unauthorized data access (IDOR), and denial-of-service. This document captures the defective behaviors, the correct behaviors that must replace them, and the existing behaviors that must be preserved throughout the fix.

---

## Bug Analysis

### Current Behavior (Defect)

**Secrets Management**

1.1 WHEN the application is built and committed to version control THEN the system exposes production database credentials, SendGrid API key, Cloudinary API secret, Paymongo secret key and webhook secret, Microsoft Graph client secret, and Turnstile secret key as plain text in `appsettings.json` and `appsettings.Production.json`.

1.2 WHEN `DbInitializer.cs` runs on startup THEN the system stores all default user passwords (SuperAdmin123!, Admin123!, Sales123!, Staff123!, Support123!, Marketing123!, Billing123!, Customer123!) as plain-text string literals in a hardcoded dictionary visible to anyone with repository access.

**Authentication — Account Lockout**

1.3 WHEN a user submits repeated failed login attempts THEN the system does not increment a lockout counter and does not lock the account, because `PasswordSignInAsync` is called with `lockoutOnFailure: false`, leaving the login endpoint fully open to brute-force attacks.

**Insecure Direct Object Reference (IDOR) — Customer Portal**

1.4 WHEN an authenticated Customer user accesses `CustomerPortalController` actions (Dashboard, MyOrders, MyInvoices, PayWithPaymongo, PaymentSuccess, ArchiveInvoice) THEN the system resolves the customer record by matching `User.Identity.Name` (the login email) against `Customer.Email` with no `UserId` foreign key binding, allowing an attacker who manipulates session state or shares an email to access another customer's orders and invoices.

1.5 WHEN the `Customer` model is created or seeded THEN the system does not persist a `UserId` field linking the `Customer` record to its owning `ApplicationUser`, so ownership cannot be enforced at the database level.

**OAuth Token Storage**

1.6 WHEN a Sales Staff user completes the Microsoft Graph OAuth flow THEN the system stores the raw access token in plain-text ASP.NET session via `HttpContext.Session.SetString($"GraphToken_{userId}", accessToken)`, with no encryption or secure token store.

**Security Headers**

1.7 WHEN the application responds to any HTTP request THEN the system omits the `Content-Security-Policy`, `Strict-Transport-Security` (HSTS for non-development), and `Permissions-Policy` response headers, leaving browsers without key client-side protections.

**File Upload Validation**

1.8 WHEN a user uploads a file through `SupportController.Create`, `CustomerPortalController.MyProfile`, or `ProductsController.Create`/`Edit` THEN the system passes the file directly to Cloudinary with no MIME type or file extension whitelist check and no server-side file size limit, allowing arbitrary file types and oversized uploads.

**Audit Logging**

1.9 WHEN a user fails authentication (wrong password, locked account, Turnstile failure) THEN the system does not write an audit log entry for the failed attempt.

1.10 WHEN an authenticated user is denied access due to an authorization failure (wrong role, deactivated account) THEN the system does not write an audit log entry for the authorization failure.

1.11 WHEN a privileged operation is performed (password reset, role change, user deactivation/reactivation) THEN the system does not write a dedicated audit log entry capturing the actor, target user, and operation performed.

**Geolocation HTTP Client**

1.12 WHEN a user logs in from a non-localhost IP address THEN the system creates a new `HttpClient` instance inline and calls the external `ip-api.com` geolocation endpoint with no timeout configured, causing the login request to hang indefinitely if the external service is slow or unreachable.

**Model Input Validation**

1.13 WHEN a user submits a form to create or edit an `Opportunity` THEN the system accepts negative `EstimatedValue` amounts and `Probability` values outside 0–100 because the `Opportunity` model has no `[Range]` validation attributes on these fields.

1.14 WHEN a user submits a form to create or edit a `Lead` THEN the system accepts phone numbers and company names of unlimited length because the `Lead` model has no `[StringLength]` attributes on `Phone`, `Company`, `Source`, or `Status` fields.

1.15 WHEN a user submits a form to create or edit a `SupportTicket` THEN the system accepts subject and description fields of unlimited length because the `SupportTicket` model has no `[StringLength]` attributes on `Subject` or `Description`.

**SupportController Authorization**

1.16 WHEN any authenticated user (regardless of role) accesses `SupportController.Index` THEN the system returns all support tickets in the system, because the controller uses a broad `[Authorize]` attribute with no role restriction, allowing roles such as Customer or Sales Staff to view all tickets.

**N+1 Queries and In-Memory Filtering**

1.17 WHEN `CustomersController.Index` is called THEN the system loads all customers from the database and then filters by `IsActive` in application memory rather than at the database level, causing unnecessary data transfer for large datasets.

1.18 WHEN `SalesManagerController.Dashboard` calculates team performance THEN the system loads all "Closed Won" opportunities into memory and then groups and aggregates them in application code rather than using a database-level `GroupBy` query.

**Generic Exception Catching**

1.19 WHEN an exception occurs during `CustomersController.Edit`, `BillingController.Edit`, or `ProductsController.Edit` THEN the system catches the generic `Exception` type rather than the specific `DbUpdateConcurrencyException`, masking the true error type and making debugging harder.

---

### Expected Behavior (Correct)

**Secrets Management**

2.1 WHEN the application is built and committed to version control THEN the system SHALL store all secrets (database password, SendGrid API key, Cloudinary API secret, Paymongo keys, Microsoft Graph client secret, Turnstile secret key) exclusively in environment variables or a secrets manager (e.g., ASP.NET User Secrets for development, environment variables or Azure Key Vault for production), and `appsettings.json` / `appsettings.Production.json` SHALL contain only non-sensitive placeholder references or be absent of secret values entirely.

2.2 WHEN `DbInitializer.cs` runs on startup THEN the system SHALL NOT contain hardcoded default passwords in source code; seed passwords SHALL be sourced from environment variables or a secrets provider, and the `UpdateUserPasswords` method that force-resets passwords on every startup SHALL be removed.

**Authentication — Account Lockout**

2.3 WHEN a user submits repeated failed login attempts THEN the system SHALL call `PasswordSignInAsync` with `lockoutOnFailure: true`, and ASP.NET Identity lockout SHALL be configured with a maximum failed-attempt threshold (e.g., 5 attempts) and a lockout duration (e.g., 15 minutes) so that accounts are temporarily locked after the threshold is exceeded.

**Insecure Direct Object Reference (IDOR) — Customer Portal**

2.4 WHEN an authenticated Customer user accesses any `CustomerPortalController` action that retrieves or modifies customer-owned data THEN the system SHALL resolve the customer record using a `UserId` foreign key that matches the authenticated user's `ApplicationUser.Id`, and SHALL return a 403 or 404 response if no matching record is found for that user ID.

2.5 WHEN a `Customer` record is created for a portal user THEN the system SHALL persist a `UserId` field on the `Customer` entity that stores the corresponding `ApplicationUser.Id`, and the `DbInitializer` seeding logic SHALL populate this field for the default customer seed account.

**OAuth Token Storage**

2.6 WHEN a Sales Staff user completes the Microsoft Graph OAuth flow THEN the system SHALL store the access token using ASP.NET Data Protection (`IDataProtector`) or an equivalent encrypted store, and SHALL NOT store the raw token as a plain-text session string.

**Security Headers**

2.7 WHEN the application responds to any HTTP request THEN the system SHALL include a `Content-Security-Policy` header with a restrictive policy appropriate for the application's content, a `Permissions-Policy` header disabling unused browser features, and (in non-development environments) a `Strict-Transport-Security` header with an appropriate `max-age`.

**File Upload Validation**

2.8 WHEN a user uploads a file through any controller action that accepts `IFormFile` THEN the system SHALL validate that the file's content type and extension are within an explicit whitelist of permitted image types (e.g., `image/jpeg`, `image/png`, `image/gif`, `image/webp`) and SHALL reject uploads that exceed a defined maximum file size (e.g., 5 MB) before passing the file to Cloudinary, returning a validation error to the user if either check fails.

**Audit Logging**

2.9 WHEN a user fails authentication (wrong password, Turnstile failure, or account lockout) THEN the system SHALL write an audit log entry recording the attempted email, failure reason, timestamp, and IP address.

2.10 WHEN an authenticated user is denied access due to an authorization failure THEN the system SHALL write an audit log entry recording the user identity, the requested resource, the denial reason, and the timestamp.

2.11 WHEN a privileged operation is performed (password reset, role assignment or removal, user deactivation, user reactivation) THEN the system SHALL write a dedicated audit log entry capturing the acting user's ID and name, the target user's ID and email, the operation type, and the timestamp.

**Geolocation HTTP Client**

2.12 WHEN a user logs in from a non-localhost IP address THEN the system SHALL use the registered `IIpGeolocationService` (which is already registered as an `HttpClient`-backed service in `Program.cs`) rather than creating an inline `HttpClient`, and the HTTP client SHALL be configured with a timeout (e.g., 3 seconds) so that a slow or unreachable geolocation service does not block the login response.

**Model Input Validation**

2.13 WHEN a user submits a form to create or edit an `Opportunity` THEN the system SHALL reject `EstimatedValue` values below 0 and `Probability` values outside the range 0–100 via `[Range]` validation attributes on the `Opportunity` model.

2.14 WHEN a user submits a form to create or edit a `Lead` THEN the system SHALL enforce maximum lengths on `Phone` (20 chars), `Company` (100 chars), `Source` (50 chars), and `Status` (20 chars) via `[StringLength]` attributes on the `Lead` model.

2.15 WHEN a user submits a form to create or edit a `SupportTicket` THEN the system SHALL enforce a maximum length of 200 characters on `Subject` and 2000 characters on `Description` via `[StringLength]` attributes on the `SupportTicket` model.

**SupportController Authorization**

2.16 WHEN a user accesses `SupportController.Index` THEN the system SHALL restrict access to roles that legitimately manage support tickets: `Super Admin`, `Admin`, and `Support Staff`. Roles such as `Customer`, `Sales Staff`, `Sales Manager`, `Marketing Staff`, `Marketing Manager`, and `Billing Staff` SHALL be denied access.

**N+1 Queries and In-Memory Filtering**

2.17 WHEN `CustomersController.Index` is called THEN the system SHALL apply the `IsActive` filter at the database level by passing the filter predicate to the service/repository query rather than loading all customers and filtering in memory.

2.18 WHEN `SalesManagerController.Dashboard` calculates team performance THEN the system SHALL perform the `GroupBy` aggregation at the database level using EF Core's `GroupBy` translation rather than loading all opportunities into memory first.

**Generic Exception Catching**

2.19 WHEN a concurrency exception occurs during `CustomersController.Edit`, `BillingController.Edit`, or `ProductsController.Edit` THEN the system SHALL catch `DbUpdateConcurrencyException` specifically rather than the generic `Exception` type, and SHALL handle it by checking record existence and rethrowing appropriately.

---

### Unchanged Behavior (Regression Prevention)

3.1 WHEN a user provides valid credentials and passes Turnstile verification THEN the system SHALL CONTINUE TO authenticate the user successfully and redirect them to their role-appropriate dashboard.

3.2 WHEN a user's account is explicitly deactivated by a Super Admin via `AdminController.DeactivateUser` THEN the system SHALL CONTINUE TO prevent that user from logging in via the existing `LockoutEnd = DateTimeOffset.MaxValue` mechanism.

3.3 WHEN a Super Admin reactivates a user via `AdminController.ReactivateUser` THEN the system SHALL CONTINUE TO restore login access by clearing the lockout end date.

3.4 WHEN an authenticated Customer user accesses `CustomerPortalController.MyTickets` THEN the system SHALL CONTINUE TO return only the tickets whose `CustomerId` matches the authenticated user's `ApplicationUser.Id` (this lookup is already ID-based and must remain so).

3.5 WHEN a PayMongo webhook is received THEN the system SHALL CONTINUE TO verify the HMAC-SHA256 signature before processing the payload, and SHALL CONTINUE TO reject requests with invalid or missing signatures.

3.6 WHEN a Sales Staff user creates, edits, or views their own leads and opportunities THEN the system SHALL CONTINUE TO enforce ownership by comparing `AssignedToUserId` against the authenticated user's ID.

3.7 WHEN any non-GET request is made by an authenticated user THEN the system SHALL CONTINUE TO write an audit log entry via `AuditLogFilter` capturing the controller, action, user identity, role, and IP address.

3.8 WHEN a user uploads a valid image file within the permitted type and size limits THEN the system SHALL CONTINUE TO upload the file to Cloudinary and persist the resulting URL.

3.9 WHEN the application starts up THEN the system SHALL CONTINUE TO apply pending EF Core migrations and seed roles and default user accounts if they do not already exist.

3.10 WHEN a user logs in from a new geographic location THEN the system SHALL CONTINUE TO send a security alert email to the user's registered email address.

3.11 WHEN the global rate limiter is active THEN the system SHALL CONTINUE TO enforce per-IP request limits and return HTTP 429 when the limit is exceeded.

3.12 WHEN a user enables two-factor authentication THEN the system SHALL CONTINUE TO require the TOTP code on subsequent logins via the existing `RequiresTwoFactor` redirect path.

---

## Bug Condition Pseudocode

### C(X) — Bug Condition Functions

```pascal
FUNCTION isSecretExposed(X)
  INPUT: X of type ConfigurationFile
  OUTPUT: boolean
  RETURN X contains literal secret values (API keys, passwords, connection string passwords)
         AND X is tracked by version control
END FUNCTION

FUNCTION isHardcodedPassword(X)
  INPUT: X of type SourceFile
  OUTPUT: boolean
  RETURN X contains a Dictionary<string, string> mapping user emails to plain-text passwords
         AND X is executed on application startup
END FUNCTION

FUNCTION isLockoutDisabled(X)
  INPUT: X of type LoginRequest
  OUTPUT: boolean
  RETURN PasswordSignInAsync is called with lockoutOnFailure = false
END FUNCTION

FUNCTION isIDORVulnerable(X)
  INPUT: X of type CustomerDataRequest
  OUTPUT: boolean
  RETURN customer record is resolved by matching User.Identity.Name to Customer.Email
         AND no UserId foreign key ownership check is performed
END FUNCTION

FUNCTION isTokenStoredInsecurely(X)
  INPUT: X of type OAuthCallbackRequest
  OUTPUT: boolean
  RETURN access token is stored via HttpContext.Session.SetString without encryption
END FUNCTION

FUNCTION isFileUploadUnvalidated(X)
  INPUT: X of type FileUploadRequest
  OUTPUT: boolean
  RETURN IFormFile is passed to Cloudinary without MIME type whitelist check
         OR IFormFile is passed to Cloudinary without server-side size limit check
END FUNCTION

FUNCTION isModelValidationMissing(X)
  INPUT: X of type ModelClass
  OUTPUT: boolean
  RETURN X has numeric or string properties without [Range] or [StringLength] validation attributes
         AND those properties are bound from user-submitted form data
END FUNCTION

FUNCTION isSupportAuthorizationTooPermissive(X)
  INPUT: X of type HttpRequest to SupportController
  OUTPUT: boolean
  RETURN request is authenticated
         AND user role is NOT in {"Super Admin", "Admin", "Support Staff"}
         AND SupportController.Index returns HTTP 200 with ticket data
END FUNCTION

FUNCTION isInMemoryFiltering(X)
  INPUT: X of type DatabaseQuery
  OUTPUT: boolean
  RETURN all records are loaded from database
         AND filtering/aggregation is performed in application memory
         rather than translated to SQL
END FUNCTION
```

### Property Specifications

```pascal
// Property: Fix Checking — Secrets Not in Source
FOR ALL X WHERE isSecretExposed(X) DO
  result ← scanConfigFile'(X)
  ASSERT result contains NO literal secret values
  ASSERT result references environment variables or secrets provider
END FOR

// Property: Fix Checking — Account Lockout Enforced
FOR ALL X WHERE isLockoutDisabled(X) DO
  result ← login'(X, failedAttempts >= lockoutThreshold)
  ASSERT result.IsLockedOut = true
  ASSERT account is temporarily inaccessible
END FOR

// Property: Fix Checking — IDOR Prevented
FOR ALL X WHERE isIDORVulnerable(X) DO
  result ← getCustomerData'(X)
  ASSERT result is resolved by UserId = ApplicationUser.Id
  ASSERT result returns 403/404 when UserId does not match authenticated user
END FOR

// Property: Fix Checking — File Upload Validated
FOR ALL X WHERE isFileUploadUnvalidated(X) DO
  result ← uploadFile'(X)
  ASSERT result rejects files with non-whitelisted MIME types
  ASSERT result rejects files exceeding maximum size limit
END FOR

// Property: Preservation Checking
FOR ALL X WHERE NOT isLockoutDisabled(X) DO
  ASSERT login'(X, validCredentials) = login(X, validCredentials)  // successful login unchanged
END FOR

FOR ALL X WHERE NOT isIDORVulnerable(X) DO
  ASSERT getCustomerData'(X, ownedRecord) = getCustomerData(X, ownedRecord)  // own data still accessible
END FOR

FOR ALL X WHERE NOT isFileUploadUnvalidated(X) DO
  ASSERT uploadFile'(X, validImage) = uploadFile(X, validImage)  // valid uploads still succeed
END FOR
```
