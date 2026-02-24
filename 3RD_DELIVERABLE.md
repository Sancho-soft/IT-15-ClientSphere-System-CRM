NAME: ALDREN LOUIE REYES
PROJECT TITLE: ClientSphere: Customer Relationship Management System
SUBJECT: IT15/L Integrative Programming and Technologies
CODE: 8441
TIME: 10:00 am - 12:00 nn

TOPIC (Type of Business Process): #5 - Customer Relationship Management (CRM)
Products/Services: Sales and Customer Service

Deployed Link / Website link:
http://clientsphereph.runasp.net

Role Based-Access:

Super Admin Acct. (if done)
Username: superadmin@clientsphere.com
Password: SuperAdmin123!

Admin Acct.
Username: admin@clientsphere.com
Password: Admin123!

Client Acct.
Username: customer@clientsphere.com
Password: Customer123!


3rd Deliverables-Deployed Transactions (CRUD)

Log in-Page
(Screenshot)

[Insert Screenshot of Login Page]

    The Login Page enables internal corporate personnel and clients to securely access the system using their assigned account credentials. Accounts are managed by the Administrator, ensuring that only authorized users can log in to the platform. Upon entering a valid username and password, the system authenticates the user’s identity and grants access based on their designated role within the organization, such as Super Admin, Admin, or Customer. This role-based authentication ensures that users are directed to the appropriate dashboard and are only able to access features and data relevant to their responsibilities. In cases of invalid login credentials, the system displays an error message.

Log in-Page  - Source Code
(Screenshot)

[Insert Screenshot of Login Source Code]
Source Code from C#_ASP. NET Core
- HTML View: `Views/Account/Login.cshtml`
- Controller: `Controllers/AccountController.cs` (Capture the `[HttpPost] public async Task<IActionResult> Login` method)


Dashboard (Client/End-User)
(Screenshot)

[Insert Screenshot of Support Dashboard]

    The Dashboard serves as the primary interface for users to manage inquiries and support requests within the system. It provides an immediate top-level overview of system health through four key metric cards: Total Tickets, In Progress, Resolved, and Critical tickets. Through this dashboard, users can track the progress of assignments and stay informed. It allows users to actively engage in the system workflow processes, with clear metrics indicating which tickets need immediate attention.

Dashboard - Source Code (Client/End-User)
(Screenshot)

[Insert Screenshot of Dashboard Source Code]
Source Code from C#_ASP. NET Core
- HTML View: `Views/Admin/Dashboard.cshtml`
- Controller: `Controllers/AdminController.cs` (Capture the `public async Task<IActionResult> Dashboard()` method)


Update/Edit Page
(Screenshot)

[Insert Screenshot of Edit Support Ticket Page]

    The Update/Edit page provides an administrative interface for staff to modify and refine their recorded issues. This page allows authorized users to securely update core ticket information, such as the Customer ID/Name, the ticket Subject, and its Description. Critical triage fields can also be adjusted here, allowing users to alter the Priority level (Low, Medium, High, Critical) and the current Status (Open, In Progress, Resolved, Closed). The interface prominently displays the exact creation date and the Last Updated timestamp, ensuring that updates are synchronized with the system’s central database for full audit transparency.

Update/Edit Page - Source Code
(Screenshot)

[Insert Screenshot of Edit Support Ticket Source Code]
Source Code from C#_ASP. NET Core
- HTML View: `Views/Support/Edit.cshtml`
- Controller: `Controllers/SupportController.cs` (Capture the `[HttpPost] public async Task<IActionResult> Edit` method)


Archive
(Screenshot)

[Insert Screenshot of Customers Tab / Archived Tickets Tab]

    The Archive functionality in ClientSphere allows administrators to hide records from the main active views without permanently deleting them from the database. This is handled dynamically in the backend C# code in two ways:
    
    1. Boolean Property Toggle (Customers): For customer records, the system uses an `IsActive` boolean property. When the administrator clicks the Archive button, the `ArchiveCustomer` HTTP POST action is triggered. The controller retrieves the customer by ID and flips the `IsActive` value (e.g., `customer.IsActive = !customer.IsActive`). The customer is then filtered and displayed in the "Inactive" tab interface, keeping their historical data safe in the database.
    
    2. String Status Update (Tickets & Invoices): For transactional records like Support Tickets and Billing Invoices, the system uses the `Status` property. When a ticket is marked as "Closed" or "Resolved" via the `CloseTicket` POST action, or an invoice is marked as "Paid", the C# LINQ queries in the controller automatically filter these records out of the Active lists and pass them to the "Archived" tabs in the view model.

Archive – Source Code
(Screenshot)

[Insert Screenshot of Archive Source Code]
Source Code from C#_ASP. NET Core
- HTML View (Customers Toggle): `Views/Customers/Index.cshtml` (Capture the ArchiveCustomer form)
- Controller (Customers Archive): `Controllers/CustomersController.cs` (Capture the `[HttpPost] ArchiveCustomer` method)


Display Records 
(Screenshot)

[Insert Screenshot of Support Tickets List]

    The Display Records interface retrieves and displays data from the SQL database using Entity Framework Core. The controller fetches the list of records asynchronously (e.g., `await _supportService.GetAllTicketsAsync()`) and passes them to the Razor View (`.cshtml`) via a View Model. In the view, a C# `@foreach` loop iterates through the model to generate the HTML table rows or cards. The view includes dynamic HTML badges that change color based on C# `if/else` conditions checking the `Status` and `Priority` properties. It also contains form buttons with `@Html.AntiForgeryToken()` for security, which trigger POST actions like `Edit`, `Delete`, `CloseTicket`, and `AddComment` back to the controller.

Display Records - Source Code 
(Screenshot)

[Insert Screenshot of Support Tickets List Source Code]
Source Code from C#_ASP. NET Core
- HTML View: `Views/Support/Index.cshtml` (Capture the Add Comment Modal and Mark as Closed buttons)
- Controller: `Controllers/SupportController.cs` (Capture the new `AddComment` and `CloseTicket` methods)


Database – SQL Server (Local)
Screenshot

[Insert Screenshot of Local SQL Server Database]

    The Database module for the CRM system is powered by Microsoft SQL Server (Local), providing a robust and structured relational storage environment for all customer-related data. Utilizing Microsoft SQL Server Management Studio (SSMS) or Visual Studio, database administrators and developers can manage the `db_clientsphere` database, which serves as the central repository for critical CRM information. The core of this storage includes the `Customers`, `SupportTickets`, and `Orders` tables, which systematically record essential attributes for every transaction, including unique identifiers, the `CompanyName`, user `Email`, and the `Description` content.

    Furthermore, the database handles data lifecycle and organizational metrics by storing the `CreatedAt` timestamps, the current `Status` (such as Open, In Progress, Resolved, or Closed), and user linkage through the `CustomerId` foreign key fields. It also features built-in logic for customer management, such as a bit-based `IsActive` column, which directly controls whether a customer record appears in the active or inactive view on the admin dashboard. By maintaining this detailed schema, the SQL Server backend ensures data integrity, supports complex filtering across different ticket priorities, and facilitates seamless Entity Framework Core synchronization between the C# user interface and the persistent storage layer.


Database – Deployed/Online
Screenshot

[Insert Screenshot of Deployed Database]

    The Database infrastructure is hosted in a Deployed/Online environment through MonsterASP.net, providing a high-availability cloud solution for the CRM system. This online implementation utilizes a remote MSSQL instance, identified as `db41223.databaseasp.net`, which mirrors the local schema to ensure seamless data transitions between development and production. Through the MonsterASP.net WebMSSQL management interface, administrators can monitor real-time database statistics, including row counts, storage sizes, and precise timestamps for the creation and last modification of table data. 

    The cloud database maintains the integrity of the organizational hierarchy and CRM workflows by hosting several critical tables, including User and Role Management tables such as `AspNetUsers` and `AspNetRoles` for secure access. The core CRM operations are meticulously tracked through the `Customers`, `SupportTickets`, and `Invoices` tables, while marketing execution data is managed via the `Campaigns` and `Leads` tables. Additionally, system auditing and tracking are handled through tables like `AuditLogs`. This cloud-based deployment ensures that all CRM data remains synchronized and accessible to remote users and staff, providing a scalable backbone for ClientSphere's digital operations.


Date Submitted:

Teacher’s Feedback

_________________________________________________________________
_________________________________________________________________
_________________________________________________________________
_________________________________________________________________

________________________________            __________________________   
Student’s Signature (after feedbacking)                       Teacher’s Signature

-Submit on time. -Feb 20, 2026 SOFTCOPY-UPLOAD using BBL
-Use this template. (A4 Size) save as PDF FILE
