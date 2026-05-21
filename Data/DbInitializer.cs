using ClientSphere.Constants;
using ClientSphere.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ClientSphere.Data
{
    public static class DbInitializer
    {
        public static async Task Initialize(IServiceProvider serviceProvider)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

                // Ensure database is created (or migrated)
                await context.Database.MigrateAsync();

                // Ensure all users have LockoutEnabled = true so lockouts actually trigger
                var allUsers = await userManager.Users.ToListAsync();
                foreach (var u in allUsers)
                {
                    if (!u.LockoutEnabled)
                    {
                        u.LockoutEnabled = true;
                        await userManager.UpdateAsync(u);
                    }
                }

                // Read seed password from environment variable (Finding #1)
                var seedPassword = Environment.GetEnvironmentVariable("SEED_PASSWORD") ?? "DefaultSeed@123!";

                await SeedRolesAsync(roleManager);
                await SeedUsersAsync(userManager, seedPassword);
                await SeedSalesDataAsync(context, userManager);
                await SeedCustomerDataAsync(context, userManager);
                await SeedMarketingDataAsync(context, userManager);
                await SeedBillingDataAsync(context);
            }
        }

        /// <summary>Seed all application roles.</summary>
        private static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
        {
            foreach (var roleName in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole(roleName));
                }
            }
        }

        /// <summary>Seed default users for each role.</summary>
        private static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager, string seedPassword)
        {
            var seedUsers = new[]
            {
                (Email: "superadmin@clientsphere.com", FirstName: "Super", LastName: "Admin", Role: Roles.SuperAdmin, IsActive: true),
                (Email: "admin@clientsphere.com", FirstName: "System", LastName: "Admin", Role: Roles.Admin, IsActive: false),
                (Email: "sales.manager@clientsphere.com", FirstName: "Sales", LastName: "Manager", Role: Roles.SalesManager, IsActive: false),
                (Email: "sales.staff@clientsphere.com", FirstName: "Sales", LastName: "Staff", Role: Roles.SalesStaff, IsActive: false),
                (Email: "support.staff@clientsphere.com", FirstName: "Support", LastName: "Staff", Role: Roles.SupportStaff, IsActive: false),
                (Email: "marketing.staff@clientsphere.com", FirstName: "Marketing", LastName: "Staff", Role: Roles.MarketingStaff, IsActive: false),
                (Email: "marketing.manager@clientsphere.com", FirstName: "Marketing", LastName: "Manager", Role: Roles.MarketingManager, IsActive: false),
                (Email: "billing.staff@clientsphere.com", FirstName: "Billing", LastName: "Staff", Role: Roles.BillingStaff, IsActive: false),
                (Email: "customer@clientsphere.com", FirstName: "John", LastName: "Doe", Role: Roles.Customer, IsActive: false),
            };

            foreach (var seed in seedUsers)
            {
                var user = await userManager.FindByEmailAsync(seed.Email);
                if (user == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = seed.Email,
                        Email = seed.Email,
                        FirstName = seed.FirstName,
                        LastName = seed.LastName,
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = seed.IsActive
                    };
                    var result = await userManager.CreateAsync(newUser, seedPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, seed.Role);
                    }
                }
                else
                {
                    // Ensure role is assigned
                    if (!await userManager.IsInRoleAsync(user, seed.Role))
                    {
                        await userManager.AddToRoleAsync(user, seed.Role);
                    }
                }
            }
        }

        /// <summary>Seed leads, opportunities, and appointments for sales staff.</summary>
        private static async Task SeedSalesDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var salesStaff = await userManager.FindByEmailAsync("sales.staff@clientsphere.com");
            if (salesStaff == null) return;

            if (!await context.Leads.AnyAsync(l => l.AssignedToUserId == salesStaff.Id))
            {
                context.Leads.AddRange(
                    new Lead { FirstName = "Michael", LastName = "Scott", Company = "Dunder Mifflin", Email = "michael@dunder.com", Status = "New", Source = "Website", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                    new Lead { FirstName = "Dwight", LastName = "Schrute", Company = "Schrute Farms", Email = "dwight@schrute.com", Status = "Contacted", Source = "Referral", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                    new Lead { FirstName = "Jim", LastName = "Halpert", Company = "Athlead", Email = "jim@athlead.com", Status = "Qualified", Source = "Cold Call", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-5) }
                );
            }

            if (!await context.Opportunities.AnyAsync(o => o.AssignedToUserId == salesStaff.Id))
            {
                context.Opportunities.AddRange(
                    new Opportunity { Name = "Paper Supply Contract", EstimatedValue = 50000, Stage = "Qualification", Probability = 20, ExpectedCloseDate = DateTime.UtcNow.AddDays(30), AssignedToUserId = salesStaff.Id },
                    new Opportunity { Name = "Office Furniture Upgrade", EstimatedValue = 15000, Stage = "Proposal", Probability = 60, ExpectedCloseDate = DateTime.UtcNow.AddDays(15), AssignedToUserId = salesStaff.Id },
                    new Opportunity { Name = "Printer Fleet Replacement", EstimatedValue = 75000, Stage = "Negotiation", Probability = 80, ExpectedCloseDate = DateTime.UtcNow.AddDays(7), AssignedToUserId = salesStaff.Id },
                    new Opportunity { Name = "Stapler Bulk Order", EstimatedValue = 500, Stage = "Closed Won", Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), AssignedToUserId = salesStaff.Id }
                );
            }

            if (!await context.Appointments.AnyAsync(a => a.OrganizerUserId == salesStaff.Id))
            {
                context.Appointments.AddRange(
                    new Appointment { Title = "Initial Consultation with Dwight", Description = "Discuss paper supply contract terms and pricing.", StartTime = DateTime.UtcNow.AddDays(1).AddHours(10), EndTime = DateTime.UtcNow.AddDays(1).AddHours(11), Location = "Schrute Farms", OrganizerUserId = salesStaff.Id, Status = "Scheduled" },
                    new Appointment { Title = "Contract Review", Description = "Review the draft agreement and sign off on final terms.", StartTime = DateTime.UtcNow.AddDays(3).AddHours(14), EndTime = DateTime.UtcNow.AddDays(3).AddHours(15), Location = "Conference Room A", OrganizerUserId = salesStaff.Id, Status = "Scheduled" },
                    new Appointment { Title = "Lunch with Jim", Description = "Casual follow-up meeting to discuss Athlead opportunity.", StartTime = DateTime.UtcNow.AddDays(-1).AddHours(12), EndTime = DateTime.UtcNow.AddDays(-1).AddHours(13), Location = "Blueberry Hill", OrganizerUserId = salesStaff.Id, Status = "Completed" }
                );
            }

            await context.SaveChangesAsync();
        }

        /// <summary>Seed customer records, orders, and support tickets.</summary>
        private static async Task SeedCustomerDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var customerUser = await userManager.FindByEmailAsync("customer@clientsphere.com");
            if (customerUser == null) return;

            // Ensure Customer record exists for this user to link Orders
            var customerRecord = await context.Customers.FirstOrDefaultAsync(c => c.Email == customerUser.Email);
            if (customerRecord != null && customerRecord.UserId == null)
            {
                customerRecord.UserId = customerUser.Id;
                await context.SaveChangesAsync();
            }
            if (customerRecord == null)
            {
                customerRecord = new Customer
                {
                    ContactName = (customerUser.FirstName ?? "John") + " " + (customerUser.LastName ?? "Doe"),
                    Email = customerUser.Email,
                    Phone = customerUser.PhoneNumber ?? "555-0123",
                    CompanyName = "Acme Corp",
                    UserId = customerUser.Id,
                    CreatedAt = DateTime.UtcNow
                };
                context.Customers.Add(customerRecord);
                await context.SaveChangesAsync();
            }

            if (!await context.Orders.AnyAsync(o => o.CustomerId == customerRecord.Id))
            {
                var products = await context.Products.ToListAsync();
                if (!products.Any())
                {
                     context.Products.AddRange(
                        new Product { Name = "Widget A", Price = 600 },
                        new Product { Name = "Gadget B", Price = 850.50m },
                        new Product { Name = "Tool C", Price = 816.50m }
                     );
                     await context.SaveChangesAsync();
                     products = await context.Products.ToListAsync();
                }
                
                var p1 = products.FirstOrDefault(p => p.Name == "Widget A");
                var p2 = products.FirstOrDefault(p => p.Name == "Gadget B");
                var p3 = products.FirstOrDefault(p => p.Name == "Tool C");

                context.Orders.AddRange(
                    new Order { CustomerId = customerRecord.Id, TotalAmount = 1200.00m, Status = OrderStatus.Processing, OrderDate = DateTime.UtcNow.AddDays(-2), OrderItems = new List<OrderItem> { new OrderItem { ProductId = p1?.Id ?? 0, Quantity = 2, UnitPrice = 600 } } },
                    new Order { CustomerId = customerRecord.Id, TotalAmount = 850.50m, Status = OrderStatus.Completed, OrderDate = DateTime.UtcNow.AddMonths(-1), OrderItems = new List<OrderItem> { new OrderItem { ProductId = p2?.Id ?? 0, Quantity = 1, UnitPrice = 850.50m } } },
                    new Order { CustomerId = customerRecord.Id, TotalAmount = 2449.50m, Status = OrderStatus.Completed, OrderDate = DateTime.UtcNow.AddMonths(-3), OrderItems = new List<OrderItem> { new OrderItem { ProductId = p3?.Id ?? 0, Quantity = 3, UnitPrice = 816.50m } } }
                );
                await context.SaveChangesAsync();
            }

            if (!await context.SupportTickets.AnyAsync(t => t.CustomerId == customerUser.Id))
            {
                context.SupportTickets.AddRange(
                    new SupportTicket { Subject = "Billing inquiry - Invoice #INV-2024-001", Description = "I have a question about the tax amount.", Status = "Resolved", Priority = "Low", CustomerId = customerUser.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), LastUpdated = DateTime.UtcNow.AddDays(-4) },
                    new SupportTicket { Subject = "Feature Request: Export Data", Description = "It would be great to export my orders to CSV.", Status = "In Progress", Priority = "Medium", CustomerId = customerUser.Id, CreatedAt = DateTime.UtcNow.AddDays(-1), LastUpdated = DateTime.UtcNow }
                );
                await context.SaveChangesAsync();
            }
        }

        /// <summary>Seed marketing campaigns.</summary>
        private static async Task SeedMarketingDataAsync(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            var marketingStaff = await userManager.FindByEmailAsync("marketing.staff@clientsphere.com");
            if (marketingStaff == null) return;

            if (!await context.Campaigns.AnyAsync(c => c.ManagedByUserId == marketingStaff.Id))
            {
                context.Campaigns.AddRange(
                    new Campaign 
                    { 
                        Name = "Spring Product Launch 2024", 
                        Type = "Email", 
                        Status = "Active", 
                        StartDate = DateTime.UtcNow.AddDays(-10), 
                        EndDate = DateTime.UtcNow.AddDays(20), 
                        Budget = 5000, 
                        TargetAudienceSize = 10000, 
                        ActualRecipients = 9500, 
                        Opens = 4500, 
                        Clicks = 1200, 
                        Conversions = 350, 
                        ManagedByUserId = marketingStaff.Id 
                    },
                     new Campaign 
                    { 
                        Name = "Newsletter Q1", 
                        Type = "Email", 
                        Status = "Completed", 
                        StartDate = DateTime.UtcNow.AddMonths(-2), 
                        EndDate = DateTime.UtcNow.AddMonths(-1), 
                        Budget = 1000, 
                        TargetAudienceSize = 5000, 
                        ActualRecipients = 4900, 
                        Opens = 2200, 
                        Clicks = 800, 
                        Conversions = 150, 
                        ManagedByUserId = marketingStaff.Id 
                    },
                    new Campaign 
                    { 
                        Name = "Social Media Blitz", 
                        Type = "Social Media", 
                        Status = "Draft", 
                        StartDate = DateTime.UtcNow.AddDays(5), 
                        EndDate = DateTime.UtcNow.AddDays(15), 
                        Budget = 2500, 
                        TargetAudienceSize = 50000, 
                        ActualRecipients = 0, 
                        Opens = 0, 
                        Clicks = 0, 
                        Conversions = 0, 
                        ManagedByUserId = marketingStaff.Id 
                    }
                );
                await context.SaveChangesAsync();
            }
        }

        /// <summary>Seed invoice data.</summary>
        private static async Task SeedBillingDataAsync(ApplicationDbContext context)
        {
            try
            {
                var customerForBilling = await context.Customers.FirstOrDefaultAsync(c => c.Email == "customer@clientsphere.com");
                if (customerForBilling == null) return;

                if (!await context.Invoices.AnyAsync())
                {
                    context.Invoices.AddRange(
                        new Invoice
                        {
                            InvoiceNumber = "INV-2024-001",
                            CustomerId = customerForBilling.Id,
                            Amount = 500.00m,
                            IssueDate = DateTime.UtcNow.AddDays(-15),
                            DueDate = DateTime.UtcNow.AddDays(15),
                            Status = "Paid",
                            PaidDate = DateTime.UtcNow.AddDays(-5),
                            PaymentMethod = "Credit Card"
                        },
                        new Invoice
                        {
                            InvoiceNumber = "INV-2024-002",
                            CustomerId = customerForBilling.Id,
                            Amount = 1200.00m,
                            IssueDate = DateTime.UtcNow.AddDays(-5),
                            DueDate = DateTime.UtcNow.AddDays(25),
                            Status = "Sent",
                            PaymentMethod = "Bank Transfer"
                        },
                         new Invoice
                        {
                            InvoiceNumber = "INV-2024-003",
                            CustomerId = customerForBilling.Id,
                            Amount = 75.50m,
                            IssueDate = DateTime.UtcNow.AddDays(-2),
                            DueDate = DateTime.UtcNow.AddDays(28),
                            Status = "Pending",
                            PaymentMethod = "PayPal"
                        }
                    );
                    await context.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error seeding invoices: {ex.Message}");
            }
        }
    }
}
