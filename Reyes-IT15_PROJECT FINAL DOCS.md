# FINAL PROJECT DOCUMENTATION

**NAME:** ALDREN LOUIE L. REYES
**PROJECT TITLE:** ClientSphere: Customer Relationship Management System
**SUBJECT / CODE / TIME:** IT15/L Integrative Programming and Technologies / 8441 / 10:00 AM - 12:00 PM
**TOPIC (Type of Business Process):** #5 Customer Relationship Management (CRM)
**Products/Services:** Sales and Customer Service
**Website/Deployed (Link):** [https://clientsphereph.runasp.net](https://clientsphereph.runasp.net)

**API/ALGO/MODEL:**
*   **APIs Integrated:** 
    *   **PayMongo API:** For secure payment processing, invoicing workflows, and transaction status webhooks.
    *   **SendGrid API:** Used for reliable delivery of transactional emails and marketing campaign distributions.
    *   **Cloudinary API:** Utilized for secure cloud storage and delivery of user profile images and system media.
    *   **Microsoft Graph API:** Enables seamless Single Sign-On (SSO) and OAuth 2.0 authentication for staff accounts via Azure Active Directory.
*   **Algorithms:** Search and Sort Algorithms (for dynamic data tables and filtering).

**Security Features:**
*   User Authentication (Secure Login/Logout via ASP.NET Core Identity)
*   Role-Based Access Control (Admin, Sales, Marketing, Support, Billing, Customer)
*   Input Validation (Protection against SQL Injection and XSS using Entity Framework Core)
*   Data Encryption (Secure storage of passwords using BCrypt/Identity Hash)
*   Activity Logging and Monitoring (Audit Trails)
*   SSL/HTTPS Encryption for secure data transmission

**Target User/s:**
Super Admin, Admin, Sales Manager, Sales Staff, Marketing Manager, Marketing Staff, Support Staff, Billing Staff, and Customer.

**Sub Systems / Management Transaction/Modules:**
1. Customer Management
2. Sales and Invoicing
3. Support Ticket Management
4. Marketing Campaigns
5. Analytics and Reporting Dashboard

**Project Objectives:**
1.  **Centralized Data Mastery:** Develop a comprehensive, web-based CRM system to centralize fragmented customer data, sales records, and support interactions into one highly secure, unified platform accessible globally by authorized personnel.
2.  **Financial Automation:** Implement an automated invoicing and payment tracking module deeply integrated with the PayMongo API to streamline the billing process, automatically reconcile accounts upon customer payment, and drastically reduce manual accounting errors.
3.  **Customer Success Excellence:** Provide an efficient, priority-driven support ticketing system to track, manage, and resolve customer inquiries promptly. This ensures high service-level agreement (SLA) compliance and improves overall customer retention rates.
4.  **Data-Driven Marketing:** Enable marketing teams to design, deploy, and meticulously track email and social campaigns across specific customer segments for better engagement. Integrate with SendGrid to monitor open/click metrics and directly correlate campaigns to actual sales conversions.
5.  **Executive Visibility:** Offer real-time analytics and a consolidated command dashboard for management to track sales pipeline performance, customer satisfaction metrics, and overall business health, empowering leadership to make informed, data-driven strategic decisions.

**Project Description:**
The ClientSphere Customer Relationship Management (CRM) System is a web-based enterprise application designed to streamline customer interactions, sales processes, and support services for businesses. The system primarily targets System Administrators, Support and Marketing Staff, and the Customers themselves, each granted specific role-based access to ensure data security and operational efficiency. Organizations expect ClientSphere to provide a robust platform for managing the entire customer lifecycle—from initial lead and marketing engagement to sales conversion and post-sales support through ticket management. To enforce strict data compartmentalization, the system utilizes ASP.NET Core Identity to dynamically manage nine distinct access roles, ensuring that features like the interactive Sales Pipeline or the Marketing Campaign metrics are only accessible to authorized personnel, preventing unauthorized data exposure.

By centralizing customer data and automating workflows like invoicing and ticketing, the system is expected to significantly improve the organization's customer service response times, enhance marketing precision, and provide a clearer overview of financial performance. Teams can comprehensively track chronological customer interactions, manage highly targeted marketing efforts via SendGrid's automated email API, and ensure secure, seamless transactions via PayMongo integration. Specifically, when a customer completes an invoice payment through the gateway, a secure backend Webhook listener automatically intercepts the payload, cryptographically verifies the signature, and instantly updates the database ledger. Ultimately, the system minimizes manual data entry bottlenecks, shatters internal communication silos between departments, and empowers businesses to build stronger, data-driven relationships with their clients.

The system will be developed using C# and ASP.NET Core for robust, uncompromised backend performance, paired with HTML, CSS, JavaScript, and Bootstrap for a highly responsive frontend accessible across all devices. Microsoft SQL Server will be utilized as the primary, highly relational database management system, interacting securely via Entity Framework Core to mitigate SQL injection vulnerabilities. These core technologies were selected because they provide an enterprise-grade, highly scalable, and secure foundation capable of supporting complex role-based access logic. Furthermore, the architecture guarantees seamless integration with multiple crucial third-party APIs: PayMongo for financial compliance, SendGrid for robust email deliverability, Cloudinary CDN for offloading image storage to improve load times, and Microsoft Graph for actively synchronizing CRM appointments with live Outlook calendars within a modern CRM environment.

**Type of Users/ Role-Based Access:**
1.  **Super Admin**
    *   Username: superadmin@clientsphere.com
    *   Password: SuperAdmin123!
2.  **Admin**
    *   Username: admin@clientsphere.com
    *   Password: Admin123!
3.  **Sales Manager**
    *   Username: sales.manager@clientsphere.com
    *   Password: Sales123!
4.  **Sales Staff**
    *   Username: sales.staff@clientsphere.com
    *   Password: Staff123!
5.  **Marketing Manager**
    *   Username: marketing.manager@clientsphere.com
    *   Password: Marketing123!
6.  **Marketing Staff**
    *   Username: marketing.staff@clientsphere.com
    *   Password: Marketing123!
7.  **Support Staff**
    *   Username: support.staff@clientsphere.com
    *   Password: Support123!
8.  **Billing Staff**
    *   Username: billing.staff@clientsphere.com
    *   Password: Billing123!
9.  **Customer**
    *   Username: customer@clientsphere.com
    *   Password: Customer123!

**Data Dictionary:**
*ClientSphere CRM System*
*   **Users Table:** UserID (PK), Name, Email, PasswordHash, Role
*   **Customer Table:** CustomerID (PK), FirstName, LastName, Email, Phone, Address, City, Country, Status
*   **Sales Table:** SaleID (PK), CustomerID (FK), ItemName, Quantity, UnitPrice, TotalAmount, SaleDate
*   **SupportTicket Table:** TicketID (PK), CustomerID (FK), Subject, Description, TicketType, Status
*   **Invoices Table:** InvoiceID (PK), CustomerID (FK), Amount, DueDate, Status, PaymentMethod
*   **MarketingCampaign Table:** CampaignID (PK), CampaignName, StartDate, EndDate, Budget, Status
*   **CampaignRecipients Table:** RecipientID (PK), CampaignID (FK), CustomerID (FK), ResponseStatus
*   **Staff Table:** StaffID (PK), UserID (FK), Role

---

**Prototype (Frontend)**
*SCREENSHOTS AND DESCRIPTION (ALL TRANSACTIONS)*

*Customer Portal Module*
[Insert Screenshot: Customer Dashboard.png]
**Label Name:** Customer Secure Portal Dashboard
**Description:** The centralized landing page for clients after a successful login. It displays a comprehensive, at-a-glance summary of their account health, including active orders, recent invoices, and the status of open support tickets. The dashboard is designed for self-service, providing quick-action buttons to seamlessly create a new support ticket, track an existing order delivery, or instantly open and pay a pending invoice without needing to contact support.

[Insert Screenshot: Customer Invoice Payment.png]
**Label Name:** Secure Invoice Payment Gateway (PayMongo)
**Description:** The secure, customer-facing checkout screen. When a customer views an unpaid invoice from their portal, they are directed to this payment interface. Here, they can review the itemized billing details and select their preferred payment method (e.g., Credit Card, GCash, or PayMaya). Clicking the "Pay Now" button seamlessly initiates a secure session with the PayMongo API, ensuring that all sensitive financial data is processed off-site and never stored directly on the ClientSphere servers.

*Sales & Order Management Module*
[Insert Screenshot: Sales Dashboard.png]
**Label Name:** Sales Manager Analytics Dashboard
**Description:** The primary operational interface for Sales staff and managers, featuring a visual pipeline of prospective Leads, active Opportunities, and successfully closed Deals. It includes interactive, real-time charts tracking monthly revenue streams, sales growth, and lead conversion rates. This empowers the sales team to quickly identify bottlenecks in their funnel, forecast future revenue, and prioritize high-value client opportunities.

[Insert Screenshot: Order Processing.png]
**Label Name:** Product & Order Management Validation
**Description:** The core transaction screen where Sales or Admin staff process and finalize new customer orders. Users can select products from the dynamic inventory catalog, adjust purchase quantities, and apply any necessary discounts. The system automatically calculates the subtotal, taxes, and final billing amount while concurrently verifying that sufficient product stock exists in the database before the order can be confirmed and saved.

*Marketing & Campaign Module*
[Insert Screenshot: Marketing Campaigns.png]
**Label Name:** Marketing Campaign Tracker & Metrics
**Description:** The dedicated interface where Marketing staff create, schedule, and monitor the performance of email or social media outreach campaigns. It lists all active and historical campaigns in a grid, showcasing vital real-time metrics such as the Target Audience Size, successful Deliveries, Email Opens, Link Clicks, and actual Customer Conversions. This data-driven view allows the marketing team to evaluate campaign ROI and refine future targeting strategies.

*Support & Ticketing Module*
[Insert Screenshot: Support Ticket System.png]
**Label Name:** Customer Support Ticketing Interface
**Description:** The central workspace where Support Staff manage and resolve client issues. The screen displays a prioritized queue of active tickets, strategically color-coded by urgency (High/Red, Medium/Yellow, Low/Green) to ensure critical issues are addressed first. Clicking into a ticket reveals the full conversation history, allowing staff to communicate directly with the client, attach troubleshooting files, and formally transition the ticket status (e.g., from "In Progress" to "Resolved").

*Billing & Finance Module*
[Insert Screenshot: Billing Invoices.png]
**Label Name:** Invoice Generation and Finance Tracking
**Description:** The financial module utilized by Billing Staff to generate formal invoices for processed customer orders. It features a comprehensive, searchable data table of all system invoices, detailing the associated customer, due dates, outstanding balances, and real-time payment statuses (e.g., Pending, Sent, Paid, Overdue). This screen is crucial for maintaining cash flow visibility and triggering automated payment reminders for overdue accounts.

*System Administration Module*
[Insert Screenshot: Admin Dashboard.png]
**Label Name:** Main Administrator Dashboard & User Management
**Description:** The highly restricted, central command hub exclusively accessible to System Administrators. It aggregates data across all modules to display overall system health, total revenue, and active staff counts. Crucially, it houses the User Access Management table, allowing admins to provision new staff accounts, permanently deactivate departed employees, strictly assign access roles (e.g., Sales vs. Support), and configure global system settings to enforce organizational policies.

---

**Prototype (Backend)**
*SCREENSHOTS AND DESCRIPTION (Source Code)*

[Insert Screenshot: DbContext Configuration.png]
**Label Name:** Entity Framework Core Database Context Models
**API/Algo Usage:** Utilizes EF Core Object-Relational Mapping (ORM).
**Description:** This backend source code defines the core `ApplicationDbContext` and maps C# entity classes (e.g., Customers, Invoices, SupportTickets) directly to SQL Server database tables. It highlights the use of strongly-typed `DbSet` properties, the configuration of complex foreign key relationships, and cascade delete rules. This architecture abstracts raw SQL queries, enabling developers to interact with the database using safe LINQ expressions while ensuring strict data integrity during complex multi-table transaction commits.

[Insert Screenshot: Role Manager Seeding.png]
**Label Name:** ASP.NET Core Identity Role Based Seeding
**API/Algo Usage:** Utilizes ASP.NET Core Identity API.
**Description:** This code block demonstrates the `DbInitializer` logic executed during application startup. The system automatically inspects the database and provisions the foundational access hierarchy, creating the nine mandatory roles (e.g., Super Admin, Sales Manager, Customer). It then seeds the default testing user accounts, utilizing Identity's built-in cryptography API to securely hash passwords before insertion, guaranteeing that a secure baseline access control is enforced immediately upon deployment.

[Insert Screenshot: Repository Pattern.png]
**Label Name:** Data Access Repository Pattern
**Description:** Illustrates the implementation of the Repository Pattern (e.g., `CustomerRepository` implementing `ICustomerRepository`). This crucial architectural layer encapsulates all raw database querying logic, effectively isolating the frontend controllers from the EF Core `_context`. This approach promotes maximum code reusability, simplifies unit testing by allowing mock databases to be injected, and strictly adheres to the Separation of Concerns principle across the enterprise application.

[Insert Screenshot: Service Layer Logic.png]
**Label Name:** Business Logic Service Layer
**Description:** Highlights a backend Service class (e.g., `OrderService`). The Service Layer acts as the operational brain of the application, existing between the Controllers and Repositories. It computes all critical business rules before any data is saved. For instance, when processing an order, this layer performs real-time stock validations, calculates volume discounts, and generates accurate tax subtotals, ensuring that complex, proprietary calculations are executed safely strictly on the server side.

[Insert Screenshot: Audit Log Filter.png]
**Label Name:** Global Action Filter Audit Logging
**Description:** Demonstrates the implementation of the custom `AuditLogFilter` class utilizing ASP.NET Core MVC Action Filters. This is a global security interceptor that automatically executes on every critical HTTP request (Create, Update, Delete) initiated by any authenticated user. It silently captures the user's active session ID, the target controller action, and precise timestamps, asynchronously saving this data to the `AuditLogs` table to maintain strict security compliance and operational traceability.

---

**API FUNCTIONS / FEATURES**
*SCREENSHOTS AND DESCRIPTION (Source Code)*

[Insert Screenshot: PayMongo Controller.png]
**Label Name:** PayMongo Webhook Secure Payment Listener
**API/Algo Usage:** Integrates the third-party PayMongo Webhook API.
**Description:** Shows the backend `WebhookController` exposing a publicly accessible, yet digitally secure endpoint. This process is triggered autonomously by PayMongo's servers whenever a customer successfully completes a transaction. The C# code captures the inbound HTTP POST request, cryptographically verifies the payload signature utilizing a private `WebhookSecret` to prevent spoofing attacks, and subsequently locates the corresponding invoice in the database to update its status to "Paid", achieving fully automated revenue tracking.

[Insert Screenshot: SendGrid Service.png]
**Label Name:** SendGrid SMTP Email Dispatcher Logic
**API/Algo Usage:** Integrates the SendGrid REST API (`SendGridClient`).
**Description:** Illustrates the C# email service architecture utilizing the official SendGrid NuGet package. Triggered globally by events such as invoice generation or ticket resolution, this asynchronous routine constructs a formal multi-part MIME email object, injects context-specific dynamic variables (e.g., the customer's name or secure payment link), and dispatches it over HTTPS. This bypasses unreliable local SMTP servers, guaranteeing rapid inbox delivery and built-in tracking capabilities.

[Insert Screenshot: Cloudinary Upload.png]
**Label Name:** Cloudinary Cloud Media Management Stream
**API/Algo Usage:** Integrates the Cloudinary API.
**Description:** Demonstrates the optimized logic for securely processing binary image uploads (such as user avatars or support ticket attachments). Rather than storing heavy binary blobs in the SQL database, the system validates the file MIME type on the server and streams the raw byte data directly to Cloudinary's content delivery network (CDN). Cloudinary processes the upload and instantly returns a secure, fast-loading public URL string, significantly improving system load times and reducing local storage overhead.

[Insert Screenshot: Microsoft Graph Calendar.png]
**Label Name:** Microsoft Graph API Calendar Synchronization
**API/Algo Usage:** Integrates the Microsoft Graph API via MSAL.
**Description:** Highlights the backend `GraphCalendarService` logic engineered to synchronize CRM Appointments bi-directionally with a Sales Staff member's actual Azure/Office 365 Outlook Calendar. Utilizing the Microsoft Authentication Library (MSAL), the system acquires an OAuth 2.0 access token via a secure client-credentials flow, effectively pushing `CreateEventAsync` requests directly to Microsoft's servers to guarantee calendar alignment across the entire workforce.

---

**SECURITY FEATURES**
*SCREENSHOTS AND DESCRIPTION (Source Code)*

[Insert Screenshot: Authorization Attributes.png]
**Label Name:** Controller Action Role Limitations (RBAC)
**Description:** Visualizes the deployment of `[Authorize(Roles = "Super Admin, Admin")]` data annotations above specific, sensitive backend endpoint methods. This robust security process works by having the ASP.NET Core Identity middleware intercept incoming HTTP requests before business logic is executed. The system extracts and decrypts the user's session cookie containing their `ClaimsPrincipal` role claims; if the required role is strictly missing, the request pipeline is instantly terminated with an HTTP 403 Forbidden Access error, nullifying unauthorized execution vectors.

[Insert Screenshot: Password Identity Hashing.png]
**Label Name:** ASP.NET Core Identity Cryptographic Password Hashing
**Description:** Highlights the critical, secure user registration and password reset sequence inside the `OnPostAsync` methods. Instead of saving plaintext passwords, the system delegates to Identity’s built-in `PasswordHasher`. Upon account creation, the algorithm automatically generates a unique cryptographic salt and applies a computationally expensive, iterated one-way digest (functionally equivalent to PBKDF2). Consequently, even in a scenario where the database is fully compromised, the raw plaintext passwords remain statistically undecipherable against brute-force and rainbow table attacks.

[Insert Screenshot: CSRF Anti-Forgery.png]
**Label Name:** Cross-Site Request Forgery (CSRF) Mitigation Tokens
**Description:** Showcases the strategic placement of the `[ValidateAntiForgeryToken]` attribute across all mutating HTTP POST methods. This fundamental security mechanism prevents cross-site request forgery attacks by forcing the client's browser to submit a cryptographically bound synchronization token, generated by the server layout form, alongside the session cookie. The server validates that both matched pairs originated from the same trusted domain configuration, rendering malicious third-party form submissions futile.

[Insert Screenshot: EF Core Parameterization.png]
**Label Name:** SQL Injection Prevention via Entity Framework Parameterization
**Description:** Demonstrates how all database queries are strictly constructed utilizing Entity Framework Core (e.g., `await _context.Customers.FirstOrDefaultAsync(c => c.Id == id)`). Unlike vulnerable legacy approaches that rely on raw string concatenation, C# LINQ queries are fundamentally immune to traditional SQL Injection. The underlying EF Core database provider parses the expression tree and strictly translates input values as `DbParameter` objects, neutralizing any malicious SQL commands injected into user input fields before the query reaches the SQL Server compiler.
