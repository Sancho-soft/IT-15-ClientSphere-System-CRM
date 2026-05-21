# Requirements Document

## Introduction

This feature addresses two related concerns in the ClientSphere ASP.NET Core MVC CRM application.

**Part 1** fixes several system settings that are saved to the database but never applied at runtime: session timeout, minimum password length, the "Send Test Email" button, the "Regenerate" API key button, and the "Backup Now" / "View Backups" buttons — all of which are currently non-functional dead buttons or ignored values.

**Part 2** introduces a full data backup and restore capability accessible from Admin > System Settings > Database & Backup. Admins and Super Admins can trigger a JSON export of all application data streamed directly to the browser. Super Admins can also upload and restore a previously exported backup file, with validation and a confirmation step before any data is overwritten.

---

## Glossary

- **AdminController**: The existing ASP.NET Core MVC controller at `Controllers/AdminController.cs` that handles all admin-area actions.
- **ApplicationDbContext**: The Entity Framework Core database context at `Data/ApplicationDbContext.cs`.
- **ApplicationUser**: The ASP.NET Identity user entity.
- **BackupHistory**: A new database table that records metadata about each backup operation (timestamp, file size, triggering user, status). No backup file content is stored server-side.
- **BackupPackage**: The in-memory JSON structure that contains all exported application data, serialized and streamed to the browser as a `.json` file.
- **IEmailService**: The existing `Services.IEmailService` interface backed by `SendGridEmailService`.
- **ISystemSettingService**: The existing `Services.ISystemSettingService` interface backed by `SystemSettingService`.
- **PasswordValidator**: The ASP.NET Identity `PasswordOptions` configuration applied at application startup and refreshed at runtime.
- **RateLimitCacheService**: The existing singleton service that holds the current API rate limit in memory for dynamic enforcement.
- **SessionMiddleware**: The ASP.NET Core session middleware configured in `Program.cs` via `builder.Services.AddSession(...)`.
- **Super Admin**: A user assigned the "Super Admin" role. Has all Admin capabilities plus restore access.
- **Admin**: A user assigned the "Admin" role. Can trigger backups but cannot perform restores.
- **SystemSettingsViewModel**: The existing view model at `ViewModels/SystemSettingsViewModel.cs` bound to the System Settings form.
- **SystemSetting**: The existing model at `Models/SystemSetting.cs` with Key, Value, Group, and LastUpdatedAt fields.

---

## Requirements

### Requirement 1: Apply Session Timeout Setting at Runtime

**User Story:** As an Admin, I want the session timeout I configure in System Settings to actually take effect, so that user sessions expire after the configured idle period rather than always after 30 minutes.

#### Acceptance Criteria

1. WHEN the application starts, THE SessionMiddleware SHALL read the `SessionTimeoutMinutes` value from the database via `ISystemSettingService` and apply it as the session idle timeout.
2. WHEN an Admin saves a new `SessionTimeoutMinutes` value in System Settings, THE AdminController SHALL persist the value to the database and update the in-memory session timeout so that subsequent sessions use the new value.
3. IF the `SessionTimeoutMinutes` value is absent from the database or cannot be parsed as a positive integer, THEN THE SessionMiddleware SHALL fall back to a default idle timeout of 30 minutes.
4. THE `SessionTimeoutMinutes` setting SHALL only accept integer values between 1 and 1440 inclusive.

---

### Requirement 2: Apply Minimum Password Length Setting at Runtime

**User Story:** As an Admin, I want the minimum password length I configure in System Settings to be enforced by ASP.NET Identity, so that new passwords must meet the configured length requirement.

#### Acceptance Criteria

1. WHEN the application starts, THE PasswordValidator SHALL read the `MinimumPasswordLength` value from the database via `ISystemSettingService` and set `PasswordOptions.RequiredLength` accordingly.
2. WHEN an Admin saves a new `MinimumPasswordLength` value in System Settings, THE AdminController SHALL persist the value to the database and update the in-memory `PasswordOptions.RequiredLength` so that subsequent password operations enforce the new length.
3. IF the `MinimumPasswordLength` value is absent from the database or cannot be parsed as an integer in the range 6–128, THEN THE PasswordValidator SHALL fall back to a minimum password length of 8 characters.
4. THE `MinimumPasswordLength` setting SHALL only accept integer values between 6 and 128 inclusive.

---

### Requirement 3: Send Test Email

**User Story:** As an Admin, I want to send a test email from the System Settings page, so that I can verify the configured SMTP/SendGrid settings are working before relying on them for real notifications.

#### Acceptance Criteria

1. WHEN an Admin clicks the "Send Test Email" button, THE AdminController SHALL send a test email to the `FromEmailAddress` configured in System Settings using `IEmailService`.
2. WHEN the test email is sent successfully, THE AdminController SHALL return a success message indicating the email was delivered to the configured address.
3. IF `IEmailService` throws an exception or returns a failure result, THEN THE AdminController SHALL return an error message describing the failure without throwing an unhandled exception.
4. THE "Send Test Email" action SHALL be restricted to users in the "Super Admin" or "Admin" roles.
5. THE test email SHALL include a subject of "ClientSphere Test Email" and a body confirming the email configuration is working.

---

### Requirement 4: Regenerate API Key

**User Story:** As an Admin, I want to regenerate the system API key from the System Settings page, so that I can rotate the key when needed without manually editing the database.

#### Acceptance Criteria

1. WHEN an Admin clicks the "Regenerate" button, THE AdminController SHALL generate a new cryptographically random API key of at least 32 bytes encoded as a URL-safe Base64 string.
2. WHEN the new API key is generated, THE AdminController SHALL persist it to the database under the `SystemApiKey` setting key via `ISystemSettingService`.
3. WHEN the new API key is saved, THE AdminController SHALL return the new key value so the System Settings page displays it immediately.
4. THE "Regenerate" action SHALL be restricted to users in the "Super Admin" or "Admin" roles.
5. THE "Regenerate" action SHALL be protected by anti-forgery token validation.

---

### Requirement 5: Backup Now — Export All Application Data

**User Story:** As an Admin or Super Admin, I want to trigger an immediate backup of all application data, so that I can download a complete snapshot of the system for safekeeping or migration.

#### Acceptance Criteria

1. WHEN an Admin or Super Admin clicks the "Backup Now" button, THE AdminController SHALL export all application data into a `BackupPackage` JSON structure and stream it directly to the browser as a file download.
2. THE `BackupPackage` SHALL include all records from the following tables: ApplicationUsers (with hashed passwords, roles, and profile data), Customers, Orders, OrderItems, Leads, Opportunities, Appointments, SupportTickets, Campaigns, Invoices, PaymentRecords, AuditLogs, SystemSettings, Notifications.
3. THE AdminController SHALL name the downloaded file using the pattern `clientsphere-backup-{yyyy-MM-dd-HHmmss}.json` where the timestamp reflects the UTC time of the backup operation.
4. WHEN the backup file is streamed to the browser, THE AdminController SHALL record a new entry in the `BackupHistory` table containing: the UTC timestamp, the file size in bytes, the ID and username of the triggering user, and a status of "Completed".
5. IF an exception occurs during data export, THEN THE AdminController SHALL record a `BackupHistory` entry with status "Failed" and return an error message to the user.
6. THE backup file SHALL NOT be stored on the server; it SHALL be streamed directly to the browser response.
7. THE "Backup Now" action SHALL be restricted to users in the "Super Admin" or "Admin" roles.
8. THE "Backup Now" action SHALL be protected by anti-forgery token validation.

---

### Requirement 6: View Backup History

**User Story:** As an Admin or Super Admin, I want to view a list of past backup operations, so that I can see when backups were taken, who triggered them, and whether they succeeded.

#### Acceptance Criteria

1. WHEN an Admin or Super Admin clicks the "View Backups" button, THE AdminController SHALL navigate to a Backup History page that lists all entries in the `BackupHistory` table.
2. THE Backup History page SHALL display for each entry: the UTC timestamp formatted as `yyyy-MM-dd HH:mm:ss`, the file size in a human-readable format (e.g., "2.4 MB"), the username of the user who triggered the backup, and the status ("Completed" or "Failed").
3. THE Backup History list SHALL be ordered by timestamp descending, with the most recent backup shown first.
4. THE "View Backups" action SHALL be restricted to users in the "Super Admin" or "Admin" roles.

---

### Requirement 7: BackupHistory Table

**User Story:** As a system, I need a persistent record of all backup operations, so that admins can audit when backups were taken and whether they succeeded.

#### Acceptance Criteria

1. THE ApplicationDbContext SHALL include a `BackupHistory` DbSet backed by a new `BackupHistory` entity with the following fields: `Id` (int, primary key, auto-increment), `Timestamp` (DateTime, UTC), `FileSizeBytes` (long), `TriggeredByUserId` (string), `TriggeredByUserName` (string), `Status` (string: "Completed" or "Failed").
2. THE `BackupHistory` table SHALL be created via an Entity Framework Core migration.
3. THE `BackupHistory` table SHALL NOT store any backup file content.

---

### Requirement 8: Restore from Backup — Super Admin Only

**User Story:** As a Super Admin, I want to upload a previously downloaded backup JSON file and restore all application data from it, so that I can recover the system to a known-good state.

#### Acceptance Criteria

1. THE Backup History page SHALL provide a restore interface accessible only to users in the "Super Admin" role.
2. WHEN a Super Admin uploads a backup JSON file, THE AdminController SHALL validate that the file is a well-formed JSON document matching the expected `BackupPackage` schema before proceeding.
3. IF the uploaded file is not valid JSON or does not match the `BackupPackage` schema, THEN THE AdminController SHALL return a descriptive validation error and SHALL NOT modify any existing data.
4. WHEN the uploaded file passes validation, THE AdminController SHALL present a confirmation step to the Super Admin before executing the restore.
5. WHEN the Super Admin confirms the restore, THE AdminController SHALL replace all existing data in the database with the data from the `BackupPackage` using a full overwrite (not a merge).
6. WHEN the restore completes successfully, THE AdminController SHALL write an `AuditLog` entry with action "Data Restore", the ID and username of the Super Admin, the UTC timestamp, and the IP address of the request.
7. IF an exception occurs during the restore operation, THEN THE AdminController SHALL roll back any partial changes and return an error message to the Super Admin without leaving the database in a partially restored state.
8. THE restore action SHALL be restricted exclusively to users in the "Super Admin" role.
9. THE restore action SHALL be protected by anti-forgery token validation.

---

### Requirement 9: Backup File Round-Trip Integrity

**User Story:** As a Super Admin, I want a backup file I download to be restorable without data loss, so that the backup and restore operations are reliable.

#### Acceptance Criteria

1. FOR ALL valid application database states, exporting a `BackupPackage` and then importing it via restore SHALL produce a database state equivalent to the original state (round-trip property).
2. THE `BackupPackage` serializer SHALL produce valid JSON for any combination of application data, including records with null optional fields, Unicode text, and decimal values.
3. THE `BackupPackage` deserializer SHALL correctly parse any JSON file produced by the `BackupPackage` serializer (round-trip property: `deserialize(serialize(data)) == data`).
4. IF a `BackupPackage` JSON file was produced by a different version of the application and contains unrecognized fields, THEN THE AdminController SHALL ignore the unrecognized fields and restore only the fields it recognizes.
