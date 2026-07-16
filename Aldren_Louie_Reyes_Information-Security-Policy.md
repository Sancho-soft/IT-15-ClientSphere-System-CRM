# **ClientSphere CRM - Information Security Policy**

| ClientSphere CRM Information Technology Policy | No: CS-ISP-2026-V2 |
| :---: | :---- |
| **IT Policy: Information Security** | **Updated: Jul 7, 2026** (Original: May 2026) |
| **Class: Academic / Internal Operations** | **Issued By: Ivy Carl Benjamin, John Benedic Dutaro, Aldren Louie Reyes** <br> **Owner: Aldren Louie L. Reyes (Lead Security Architect)** |

---

# **1.0 Purpose and Benefits**

This policy defines the mandatory minimum information security requirements for the ClientSphere CRM (Customer Relationship Management) platform and its supporting infrastructure. Any entity, organization, tenant, or business unit operating under the ClientSphere CRM platform may exceed the security requirements put forth in this document, but must, at a minimum, achieve the security levels required by this policy.

This policy acts as an umbrella document to all other security policies, technical controls, and operational standards implemented within ClientSphere CRM. This policy defines the responsibility to:

* Protect and maintain the confidentiality, integrity, and availability of customer and sales data, client profiles, system configuration settings, and related infrastructure assets;
* Manage the risk of security exposure, data leakage, or credential compromise;
* Assure a secure and stable software environment for organizations managing client relations, sales leads, billing invoices, and support tickets;
* Identify, track, and respond to events involving system misuse, unauthorized data disclosure, or administrative privilege abuse;
* Monitor the system for anomalies (e.g., geographic IP login anomalies, brute force indicators) that indicate compromise; and
* Promote security awareness among workforce users, administrators, and external customer users of ClientSphere CRM.

Failure to secure and protect the confidentiality, integrity, and availability of information assets in today’s highly networked environment can lead to unauthorized exposure of Personally Identifiable Information (PII) of clients, disruption of sales operations, financial losses through transaction fraud, and damage to organizational trust. 

This policy benefits ClientSphere CRM tenants by establishing a rigorous, implementable framework that details precisely how authentication, session management, data encryption, API rate limiting, and audit logging are governed.

---

# **2.0 Authority**

This policy is issued under the authority of the **Sancho-soft System Development Group** and is authored and maintained by **Aldren Louie L. Reyes (Lead Developer & Security Architect)**. Compliance with this policy is mandatory for all deployments, administrative personnel, sales and marketing staff, support teams, billing departments, and customer portal users accessing ClientSphere CRM. 

The policy has been reviewed and approved by the **Information Security Board** and **Prof. Renz Arriola** to satisfy the rigorous security requirements for the IT-16/L (Information Assurance and Security 1) second lab examination.

---

# **3.0 Scope**

This policy encompasses all software components, databases, services, and hosting configurations under the administrative control of ClientSphere CRM. Specifically, the scope includes:

1. **ClientSphere Application Services**: The core ASP.NET Core MVC application (`ClientSphere.csproj`) running on .NET.
2. **Database Infrastructure**: The Microsoft SQL Server instance accessed via Entity Framework Core (`ApplicationDbContext`), including user identities, sales leads, opportunities, billing records, and audit logs.
3. **Integrated Third-Party APIs**:
   - **Cloudflare Turnstile**: Handles bot prevention and CAPTCHA validation.
   - **Paymongo API**: Securely processes billing and invoice payments, keeping credit card numbers completely out of the core database (reducing PCI-DSS compliance scope).
   - **Cloudinary Service**: Hosts user-uploaded profile images and attachments.
   - **Gmail SMTP Service**: Sends administrative messages, password resets, and stateless Multi-Factor Authentication (MFA) codes.
   - **IP Geolocation API**: Resolves remote client IP addresses to determine physical locations, detect geographical login anomalies, and log transaction metadata.
   - **Microsoft Graph API**: Handles calendar and appointment synchronization.
4. **All Active User Categories**: Super Admins, Admins, Sales Managers, Sales Staff, Marketing Managers, Marketing Staff, Support Staff, Billing Staff, and Customer users.

---

# **4.0 Information Statement**

## **4.1 Organizational Security**

1. Information security is integrated into both the application development phase (secure coding) and system administration (operations).
2. The core system security is managed by the Lead Security Architect (Aldren Louie L. Reyes) and system administrators, who are responsible for:
   - Ensuring that security updates and patches are applied to libraries and dependencies (e.g., Entity Framework, Identity packages).
   - Reviewing system audit logs for administrative actions and status code failures (401/403 errors).
   - Managing and modifying dynamic system settings such as API Rate Limits (`ApiRateLimit`) and Session Timeouts (`SessionTimeoutMinutes`).
3. Although third-party APIs (Paymongo, Cloudinary, Microsoft Graph) are utilized, Sancho-soft retains overall responsibility for the secure handling of tenant credentials and API access tokens stored in `appsettings.json` or system environment variables.

---

## **4.2 Functional Responsibilities**

### **1. Super Administrators (Super Admin) are responsible for:**
- Managing global application settings, tenant configurations, and system-wide security variables.
- Modifying dynamic security parameters, such as changing the `MinimumPasswordLength` or setting the maximum `ApiRateLimit` per IP.
- Manually unlocking user accounts that have been locked out due to excessive failed login attempts (5 attempts).
- Overseeing the global system audit log table (`AuditLogs`).

### **2. Administrators (Admin) are responsible for:**
- Managing user account creation, activation status (`IsActive`), and assigning appropriate roles within their tenant organizations.
- Reviewing local tenant audit logs for potential insider threats or unauthorized access patterns.
- Initiating security reviews of client-identifying data and sales pipelines.

### **3. Operational Staff (Sales, Marketing, Support, Billing) are responsible for:**
- Utilizing the system in accordance with the *Acceptable Use Policy*.
- Reporting any suspicious activity (e.g., receiving unexpected 2FA OTP codes, data inconsistencies) to the Admin.
- Ensuring customer and lead details are entered accurately and classified correctly.

### **4. Customers are responsible for:**
- Securing their personal portal credentials (passwords and email accounts used for OTP).
- Reviewing their transaction logs and invoice payments (via Paymongo portal) for unauthorized charges.

---

## **4.3 Separation of Duties**

1. To prevent administrative fraud and system misuse, role-based access control (RBAC) is strictly enforced:
   - **Billing Staff** can manage invoices and process payments but cannot access marketing campaigns or sales opportunities.
   - **Sales Staff** manage leads and opportunities but have no access to billing configuration or system logs.
   - **Support Staff** manage tickets and help desks but cannot alter customer invoice statuses.
   - **System Administrators** cannot access or modify raw transaction funds. All payments are securely routed and authorized on Paymongo’s PCI-compliant servers.
2. In-house developers do not have direct access to write raw data to the production database; all database schema changes must be deployed through controlled Entity Framework Core migrations (`db.Database.MigrateAsync()`) and seeded via a designated database initializer script (`DbInitializer.cs`).
3. System auditing is decoupled from user actions. The `AuditLogFilter` automatically records all state-changing HTTP requests (POST, PUT, DELETE) executed by authenticated users, and this process runs independently in the background without user intervention or the ability to bypass it.

---

## **4.4 Information Risk Management**

1. ClientSphere CRM must undergo an information security risk assessment at least annually or when significant modifications are made to the codebase (such as adding new integrations or migrating database platforms).
2. The Secure System Development Lifecycle (SSDLC) incorporates the following checkpoints:
   - **Static Application Security Testing (SAST)**: SonarLint is used by developers during coding to identify code smells, potential null reference exceptions, and cryptographic weaknesses.
   - **Dependency Scanning**: Routine execution of `dotnet list package --outdated` to detect and patch CVEs in NuGet packages.
   - **Input Sanitation Testing**: Verification that no form fields are vulnerable to Cross-Site Scripting (XSS) or SQL Injection.

---

## **4.5 Information Classification and Handling**

All information within ClientSphere CRM is classified into three tiers to guarantee appropriate protection levels:

### **1. Tier 1: High Confidentiality (Highly Restricted)**
* **Data Elements**: User passwords (hashed via PBKDF2), stateless MFA encryption tokens (`EncryptedOtp`), Paymongo API keys, SMTP credentials, session cookies, Microsoft Graph client secrets, and IP address logs mapped to specific user access histories.
* **Handling & Controls**:
  - Raw passwords must never be stored, logged, or transmitted in plain text.
  - Encryption in transit (TLS 1.2/1.3) is mandatory for all access.
  - Storage of payment card numbers (PAN, CVV) is prohibited within the SQL database; all payment data is processed directly by Paymongo via tokenized checkouts.
  - Highly sensitive PII such as Customer Addresses are encrypted at rest in the SQL database (`nvarchar(2000)`) using Entity Framework Core Value Converters integrated with ASP.NET Core Data Protection API (`IDataProtector`).

### **2. Tier 2: Medium Confidentiality (Internal / Restricted)**
* **Data Elements**: Customer names, emails, phone numbers, corporate invoice amounts, sales pipeline values, lead contacts, support ticket descriptions, and meeting details.
* **Handling & Controls**:
  - Accessible only to authenticated users with relevant roles (e.g., Sales Staff, Billing Staff).
  - PII must not be printed or exported to unsecured public storage.
  - Changes to these records automatically trigger audit logs.

### **3. Tier 3: Low Confidentiality (Public / General)**
* **Data Elements**: Marketing campaign names, public product catalogs, pricing tiers, and public landing pages.
* **Handling & Controls**:
  - May be viewed by anonymous visitors.
  - Modifications to public-facing catalogs still require Admin authentication and are logged.

---

## **4.6 IT Asset Management**

1. All software configurations, system keys, and API credentials must be cataloged.
2. Configuration files must exclude active passwords. Development settings (`appsettings.Development.json`) and local environment files (`.env`) are excluded from version control using `.gitignore` to prevent leakage.
3. Production secrets (e.g., database passwords, Paymongo keys, Cloudinary secrets) are injected at runtime via environment variables rather than being hardcoded.

---

## **4.7 Personnel Security**

1. Newly hired staff must receive security awareness training within 30 days of onboarding. Training must highlight:
   - Preventing phishing attacks on Gmail accounts (which are critical for SMTP delivery).
   - Recognizing social engineering attempts aiming to bypass 2FA.
   - Reporting anomalies in client details.
2. Users must review and acknowledge the *Acceptable Use Policy* upon first login.
3. Access rights are immediately revoked upon employee termination. The HR department must notify System Administrators to change the user's status (`IsActive = false`) in the database, which immediately terminates all active sessions.

---

## **4.8 Cyber Incident Management**

1. **Incident Detection**:
   - Automated rate-limiter logs (triggering HTTP 429 Too Many Requests) identify denial-of-service or brute force attacks.
   - Status Code Logging: Automatic warning logs are recorded for authorization failures (HTTP 401 Unauthorized or 403 Forbidden).
   - Geographic Anomaly Alerts: The IP Geolocation Service resolves remote user logins. If a login occurs from an IP address mapped to an unexpected country or region outside the user's historical access pattern, an alert is sent.
2. **Reporting**:
   - Employees and customers must report suspected compromises, data leaks, or unusual account behavior immediately to the Security Operations Center (SOC) at **security@clientsphere.com**.
3. **Containment & Response**:
   - Accounts experiencing brute force attacks are automatically locked out for 15 minutes after 5 failed attempts.
   - Security administrators can manually revoke session cookies and disable users in the Admin panel.
   - IP address ranges identified as malicious are blocked at the Cloudflare WAF/DNS level.
4. **Recovery**:
   - Database restore operations are executed using weekly back-ups (`MyInvoices.backup`, `MyTickets.backup`, and `ACCOUNTS_BACKUP.txt`).
   - All active authentication tokens and secrets are rotated in the event of an infrastructure breach.

---

## **4.9 Physical and Environmental Security**

1. The SQL Server database is hosted on secure, enterprise-grade cloud database servers (e.g., `public.databaseasp.net`) featuring automated physical access controls, environmental monitoring, fire suppression, and redundant power supplies.
2. Direct management access to the database server is restricted via firewall rules allowing only the ClientSphere CRM web server IP address and authorized developer IP addresses.

---

## **4.10 Account Management and Access Control**

### **1. Unique Identity**
All system access must be validated through individual, unique user accounts (registered emails). Sharing of accounts is strictly prohibited.

### **2. Password Complexity**
* Enforced by ASP.NET Core Identity:
  - Minimum length: 8 characters (dynamically adjustable via the database).
  - Required characters: At least one uppercase letter, one lowercase letter, one numeric digit, and one special character.
  - Unique characters: Must include at least 1 unique character.
* **Pwned Password Validator**: Every password registration or change is scanned in real-time using `PwnedPasswordValidator<ApplicationUser>`. This queries the HIBP (Have I Been Pwned) API securely via k-Anonymity (sending only the first 5 characters of the SHA-1 hashed password). If the password appears in any public data breach, registration/change is rejected.
* Password reset links are sent via SMTP and expire after 24 hours.

### **3. Brute Force Protection (Lockout)**
* If a user enters an incorrect password **5 consecutive times**, the account is automatically locked out.
* Lockout duration is **15 minutes**.
* The failed login attempt count is reset to 0 upon a successful login.

### **4. Session and Idle Timeout**
* **Idle Timeout**: Session cookies expire after **30 minutes** of complete inactivity (adjustable dynamically via `SessionTimeoutMinutes`).
* **Secure Cookies**: All session cookies are configured with:
  - `HttpOnly = true` (prevents access by client-side Javascript, defending against XSS cookie theft).
  - `Secure = true` (enforces transmission over HTTPS only).
  - `SameSite = SameSiteMode.None` (or `Lax` depending on deployment to prevent CSRF session reuse).
* **Session IP Binding**: Custom middleware binds the active user authentication session to the remote IP address. If the request IP changes mid-session, the cookie is invalidated, the user is signed out via `SignInManager.SignOutAsync()`, the session is cleared, and the browser is redirected to the Login page to prevent session hijacking.
* Logon Banners are displayed on the main Login portal to notify users that access is monitored.

### **5. Multi-Factor Authentication (MFA)**
* Multi-Factor Authentication is enforced for all administrative and operational staff.
* **MFA Enforcer Filter**: A global action filter (`EnforceMfaFilter`) intercepts authenticated requests. If a user in the `Super Admin` or `Admin` role accesses the platform and has not configured Two-Factor Authentication, they are immediately intercepted and redirected to `/Identity/Account/Manage/TwoFactorAuthentication` to force MFA configuration.
* **Stateless Cryptographic OTP Flow**:
  1. During login, a cryptographically secure 6-digit random code is generated using `RandomNumberGenerator.GetInt32(100000, 999999)`.
  2. The system builds a stateless string payload: `Code|UserId|ExpiryUtcDateTime`.
  3. This payload is encrypted using ASP.NET Core Data Protection (`IDataProtector` initialized with the purpose `"MFA.Stateless.OTP.v1"`).
  4. The resulting encrypted token is stored as a hidden field (`EncryptedOtp`) in the client's browser form, keeping the server completely stateless.
  5. The raw 6-digit OTP is transmitted to the user's verified email address via the Gmail SMTP client (`GmailSmtpEmailService`).
  6. Upon submission, the server decrypts the `EncryptedOtp` token. The login succeeds only if decryption is successful, the code matches the user's input, the expiration timestamp (5 minutes) has not passed, and the token is bound to the correct login session ID.
* Single-use recovery codes are provided to bypass MFA in emergencies.
* Users can trust a device for 30 days, which sets a temporary encrypted cookie.

---

## **4.11 Systems Security**

### **1. Environment Segregation**
Logical segregation is enforced between development and production environments (`app.Environment.IsDevelopment()`). Developer-focused error pages (such as detailed database stacks) are disabled in production, and standard error handling routing (`app.UseExceptionHandler("/Home/Error")`) is used.

### **2. Software Hardening**
* **SQL Injection Prevention**: Database queries are written using Entity Framework Core LINQ queries, compiling into parameterized SQL queries.
* **CSRF (Cross-Site Request Forgery) Prevention**: Auto-validation of anti-forgery tokens (`AutoValidateAntiforgeryTokenAttribute`) is registered globally on all state-changing controller endpoints.
* **File Upload Security (Magic Bytes)**: The Cloudinary upload service (`CloudinaryService.cs`) implements server-side magic bytes checking. It reads the first 12 bytes of the uploaded stream and validates its file signature headers (JPEG, PNG, GIF, WEBP) to prevent MIME-type spoofing and malicious script execution.
* **Secure HTTP Headers**: The web server appends these security headers on every response:
  - `X-XSS-Protection: 1; mode=block` (forces browsers to block pages on reflected XSS detection).
  - `X-Content-Type-Options: nosniff` (prevents MIME-type sniffing).
  - `X-Frame-Options: DENY` (prevents Clickjacking by disallowing embedding in iframes).
  - `Referrer-Policy: strict-origin-when-cross-origin` (restricts referrer leakages).
  - `Content-Security-Policy (CSP)`: Enforces strict source restrictions:
    ```
    default-src 'self'; 
    script-src 'self' 'unsafe-inline' https://challenges.cloudflare.com https://cdn.jsdelivr.net; 
    style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; 
    font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; 
    img-src 'self' data: https://res.cloudinary.com; 
    frame-src https://challenges.cloudflare.com; 
    connect-src 'self'
    ```
  - `Permissions-Policy`: Disables hardware access (camera, microphone, geolocation, usb, payments, etc.) to minimize browser-based exploitation.
  - `Strict-Transport-Security (HSTS)`: Enforces HTTPS in production with a max-age of 1 year (`max-age=31536000; includeSubDomains`).

---

## **4.12 Collaborative Computing Devices**

1. Microsoft Graph API handles calendar integrations for sales schedules.
2. Synchronizations must have explicit user authorization. Remote activation of synchronization schedules without user presence is disallowed.
3. Disconnecting external calendar access must be achievable with a single action in the user profile settings.

---

## **4.13 Vulnerability Management**

1. The codebase is scanned for security defects and structural bugs using static analyzers (SonarLint).
2. Dependencies are monitored regularly. Vulnerable libraries are updated immediately to patch exposed vectors.
3. Vulnerability validation on application interfaces (Login, MFA, Registration) is conducted periodically using Postman API testing to ensure boundaries cannot be bypassed.

---

## **4.14 Operations Security**

1. Configuration settings must use vendor-supported values.
2. Workstations used to configure or access the admin panel must run active host-based firewalls, anti-virus programs, and browser-based script protections.
3. **Continuous Monitoring**:
   - The global `AuditLogFilter` captures all non-GET successful HTTP operations.
   - The log includes: UTC Timestamp, User ID, User Name + Role (e.g., `John Doe (Admin)`), Action (e.g., `POST Customers/Create`), Description, and client IP Address.
   - Logs are stored in the SQL Server database `AuditLogs` table. Audit records cannot be altered or deleted by general users.
4. Backups: Weekly automated database backups (`MyInvoices.backup`, `MyTickets.backup`, `ACCOUNTS_BACKUP.txt`) are stored in secure secondary storage. Backup restoration must be tested periodically.

---

# **5.0 Compliance**

Compliance with this policy is mandatory. Deviations are only allowed under emergency operational conditions and must be formally authorized by the Chief Information Security Officer (CISO) or Lead Security Architect (Aldren Louie L. Reyes) through a written security exception request.

Non-compliance with this policy by workforce members may result in disciplinary actions, including revocation of credentials, suspension of role duties, or termination.

---

# **6.0 Definitions of Key Terms**

| Term | Definition |
| :---- | :---- |
| **CRM** | Customer Relationship Management; the software platform designed to manage sales pipelines, billing, and support. |
| **PII** | Personally Identifiable Information; any data that could identify a specific individual (e.g., email, phone number). |
| **MFA / 2FA** | Multi-Factor Authentication / Two-Factor Authentication; verifying identity using multiple independent credentials. |
| **OTP** | One-Time Password; a temporary, single-use numeric code utilized during multi-factor authentication. |
| **Stateless MFA** | An authentication flow that does not store the OTP value on the server database, but instead carries it encrypted on the client side. |
| **PBKDF2** | Password-Based Key Derivation Function 2; a secure cryptographic hashing algorithm used to store user passwords. |
| **HIBP** | Have I Been Pwned; an online service that tracks compromised credentials. Used to check if a password was previously leaked. |
| **Paymongo** | A third-party payment gateway API used to process billing transactions securely without storing credit cards. |
| **Cloudinary** | A cloud-based service used to host, store, and manage user-uploaded images and documents. |
| **Turnstile** | Cloudflare's non-intrusive CAPTCHA alternative used to prevent brute-force automated login bots. |
| **CSP** | Content Security Policy; an HTTP response header that restricts the resources (scripts, styles, images) a browser is allowed to load. |
| **XSS** | Cross-Site Scripting; a vulnerability where malicious scripts are injected into trusted web applications. |
| **CSRF** | Cross-Site Request Forgery; an attack that forces an end user to execute unwanted actions on a web application where they're logged in. |
| **HSTS** | HTTP Strict Transport Security; a header instructing the browser to communicate with the server exclusively via HTTPS. |

---

# **7.0 Contact Information**

Submit all security inquiries, incident reports, and requests for policy exceptions to:

**Sancho-soft System Development Group**  
*Attn: Security Operations Center (SOC)*  
Email: **security@clientsphere.com**  
Lead Architect: **Aldren Louie L. Reyes (aldrenlouielreyes@gmail.com)**  
Metro Manila, Philippines

---

# **8.0 Revision History**

This standard is subject to periodic reviews to ensure relevancy against emerging cybersecurity threats.

| Date | Version | Description of Change | Reviewer |
| :---- | :---- | :---- | :---- |
| May 15, 2026 | 1.0 | Initial release of Project Security Documentation | Aldren Louie L. Reyes |
| July 7, 2026 | 2.0 | Customized policy for ClientSphere CRM implementation; added stateless Data Protection MFA details, Paymongo PCI scope reduction, HIBP Pwned validation, and secure headers. | Aldren Louie L. Reyes |
| July 7, 2026 | 2.1 | Documented implementation of magic bytes file signature checking, session IP binding middleware, Admin MFA enforcement filter, and SQL database Customer Address PII encryption. | John Benedic Dutaro, Ivy Carl Mercado Benjamin, Aldren Louie L. Reyes |

---

# **9.0 Related Documents**

* **National Institute of Standards and Technology (NIST) Special Publication 800-53**: Security and Privacy Controls for Information Systems.
* **Payment Card Industry Data Security Standard (PCI-DSS) V4.0**: Standards for secure payment processing.
* **Microsoft ASP.NET Core Security Guidelines**: Official documentation for authentication, authorization, and data protection.
* **Cloudflare Turnstile API Integration Guides**: Security validation specifications.
