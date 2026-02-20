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
                context.Database.Migrate();

                // Update User Passwords (TEMPORARY: Force passwords to email)
                await UpdateUserPasswords(userManager);

                // Seed Roles
                string[] roleNames = { "Super Admin", "Admin", "Sales Manager", "Sales Staff", "Marketing Manager", "Marketing Staff", "Support Staff", "Billing Staff", "Customer" };
                foreach (var roleName in roleNames)
                {
                    if (!await roleManager.RoleExistsAsync(roleName))
                    {
                        await roleManager.CreateAsync(new IdentityRole(roleName));
                    }
                }

                // Seed Default Super Admin User
                var superAdminEmail = "superadmin@clientsphere.com";
                var superAdminUser = await userManager.FindByEmailAsync(superAdminEmail);
                if (superAdminUser == null)
                {
                    var newSuperAdmin = new ApplicationUser
                    {
                        UserName = superAdminEmail,
                        Email = superAdminEmail,
                        FirstName = "Super",
                        LastName = "Admin",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };
                    var result = await userManager.CreateAsync(newSuperAdmin, "SuperAdmin123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newSuperAdmin, "Super Admin");
                        superAdminUser = newSuperAdmin;
                    }
                }

                if (superAdminUser != null && !await userManager.IsInRoleAsync(superAdminUser, "Super Admin"))
                {
                    await userManager.AddToRoleAsync(superAdminUser, "Super Admin");
                }

                // Seed Default Admin User
                var adminEmail = "admin@clientsphere.com";
                var adminUser = await userManager.FindByEmailAsync(adminEmail);
                if (adminUser == null)
                {
                    var newAdmin = new ApplicationUser
                    {
                        UserName = adminEmail,
                        Email = adminEmail,
                        FirstName = "System",
                        LastName = "Admin",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newAdmin, "Admin123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newAdmin, "Admin");
                        adminUser = newAdmin;
                    }
                }

                if (adminUser != null && !await userManager.IsInRoleAsync(adminUser, "Admin"))
                {
                    await userManager.AddToRoleAsync(adminUser, "Admin");
                }

                // Seed Default Sales Manager User
                var salesManagerEmail = "sales.manager@clientsphere.com";
                var salesManagerUser = await userManager.FindByEmailAsync(salesManagerEmail);
                if (salesManagerUser == null)
                {
                    var newSalesManager = new ApplicationUser
                    {
                        UserName = salesManagerEmail,
                        Email = salesManagerEmail,
                        FirstName = "Sales",
                        LastName = "Manager",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newSalesManager, "Sales123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newSalesManager, "Sales Manager");
                    }
                }
                else
                {
                    // Ensure role is assigned
                    if (!await userManager.IsInRoleAsync(salesManagerUser, "Sales Manager"))
                    {
                        await userManager.AddToRoleAsync(salesManagerUser, "Sales Manager");
                    }
                }

                // Seed Default Sales Staff User
                var salesStaffEmail = "sales.staff@clientsphere.com";
                var salesStaffUser = await userManager.FindByEmailAsync(salesStaffEmail);
                if (salesStaffUser == null)
                {
                    var newSalesStaff = new ApplicationUser
                    {
                        UserName = salesStaffEmail,
                        Email = salesStaffEmail,
                        FirstName = "Sales",
                        LastName = "Staff",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newSalesStaff, "Staff123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newSalesStaff, "Sales Staff");
                    }
                }

                // Seed Default Support Staff User
                var supportStaffEmail = "support.staff@clientsphere.com";
                var supportStaffUser = await userManager.FindByEmailAsync(supportStaffEmail);
                if (supportStaffUser == null)
                {
                    var newSupportStaff = new ApplicationUser
                    {
                        UserName = supportStaffEmail,
                        Email = supportStaffEmail,
                        FirstName = "Support",
                        LastName = "Staff",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newSupportStaff, "Support123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newSupportStaff, "Support Staff");
                    }
                }

                // Seed Default Marketing Staff User
                var marketingStaffEmail = "marketing.staff@clientsphere.com";
                var marketingStaffUser = await userManager.FindByEmailAsync(marketingStaffEmail);
                if (marketingStaffUser == null)
                {
                    var newMarketingStaff = new ApplicationUser
                    {
                        UserName = marketingStaffEmail,
                        Email = marketingStaffEmail,
                        FirstName = "Marketing",
                        LastName = "Staff",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newMarketingStaff, "Marketing123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newMarketingStaff, "Marketing Staff");
                    }
                }

                // Seed Default Marketing Manager User
                var marketingManagerEmail = "marketing.manager@clientsphere.com";
                var marketingManagerUser = await userManager.FindByEmailAsync(marketingManagerEmail);
                if (marketingManagerUser == null)
                {
                    var newMarketingManager = new ApplicationUser
                    {
                        UserName = marketingManagerEmail,
                        Email = marketingManagerEmail,
                        FirstName = "Marketing",
                        LastName = "Manager",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newMarketingManager, "Marketing123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newMarketingManager, "Marketing Manager");
                    }
                }

                // Seed Default Billing Staff User
                var billingStaffEmail = "billing.staff@clientsphere.com";
                var billingStaffUser = await userManager.FindByEmailAsync(billingStaffEmail);
                if (billingStaffUser == null)
                {
                    var newBillingStaff = new ApplicationUser
                    {
                        UserName = billingStaffEmail,
                        Email = billingStaffEmail,
                        FirstName = "Billing",
                        LastName = "Staff",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newBillingStaff, "Billing123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newBillingStaff, "Billing Staff");
                    }
                }

                // Seed Default Customer User
                var customerEmail = "customer@clientsphere.com";
                var customerUser = await userManager.FindByEmailAsync(customerEmail);
                if (customerUser == null)
                {
                    var newCustomer = new ApplicationUser
                    {
                        UserName = customerEmail,
                        Email = customerEmail,
                        FirstName = "John",
                        LastName = "Doe",
                        EmailConfirmed = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    var result = await userManager.CreateAsync(newCustomer, "Customer123!");
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newCustomer, "Customer");
                    }
                }

                // Seed Sales Staff Data (Leads & Opportunities)
                var salesStaff = await userManager.FindByEmailAsync("sales.staff@clientsphere.com");
                if (salesStaff != null)
                {
                    if (!context.Leads.Any(l => l.AssignedToUserId == salesStaff.Id))
                    {
                        context.Leads.AddRange(
                            new Lead { FirstName = "Michael", LastName = "Scott", Company = "Dunder Mifflin", Email = "michael@dunder.com", Status = "New", Source = "Website", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-1) },
                            new Lead { FirstName = "Dwight", LastName = "Schrute", Company = "Schrute Farms", Email = "dwight@schrute.com", Status = "Contacted", Source = "Referral", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-3) },
                            new Lead { FirstName = "Jim", LastName = "Halpert", Company = "Athlead", Email = "jim@athlead.com", Status = "Qualified", Source = "Cold Call", AssignedToUserId = salesStaff.Id, CreatedAt = DateTime.UtcNow.AddDays(-5) }
                        );
                    }

                    if (!context.Opportunities.Any(o => o.AssignedToUserId == salesStaff.Id))
                    {
                        context.Opportunities.AddRange(
                            new Opportunity { Name = "Paper Supply Contract", EstimatedValue = 50000, Stage = "Qualification", Probability = 20, ExpectedCloseDate = DateTime.UtcNow.AddDays(30), AssignedToUserId = salesStaff.Id },
                            new Opportunity { Name = "Office Furniture Upgrade", EstimatedValue = 15000, Stage = "Proposal", Probability = 60, ExpectedCloseDate = DateTime.UtcNow.AddDays(15), AssignedToUserId = salesStaff.Id },
                            new Opportunity { Name = "Printer Fleet Replacement", EstimatedValue = 75000, Stage = "Negotiation", Probability = 80, ExpectedCloseDate = DateTime.UtcNow.AddDays(7), AssignedToUserId = salesStaff.Id },
                            new Opportunity { Name = "Stapler Bulk Order", EstimatedValue = 500, Stage = "Closed Won", Probability = 100, ExpectedCloseDate = DateTime.UtcNow.AddDays(-2), AssignedToUserId = salesStaff.Id }
                        );
                    }

                    if (!context.Appointments.Any(a => a.OrganizerUserId == salesStaff.Id))
                    {
                        context.Appointments.AddRange(
                            new Appointment { Title = "Initial Consultation with Dwight", Description = "Discuss paper supply contract terms and pricing.", StartTime = DateTime.UtcNow.AddDays(1).AddHours(10), EndTime = DateTime.UtcNow.AddDays(1).AddHours(11), Location = "Schrute Farms", OrganizerUserId = salesStaff.Id, Status = "Scheduled" },
                            new Appointment { Title = "Contract Review", Description = "Review the draft agreement and sign off on final terms.", StartTime = DateTime.UtcNow.AddDays(3).AddHours(14), EndTime = DateTime.UtcNow.AddDays(3).AddHours(15), Location = "Conference Room A", OrganizerUserId = salesStaff.Id, Status = "Scheduled" },
                            new Appointment { Title = "Lunch with Jim", Description = "Casual follow-up meeting to discuss Athlead opportunity.", StartTime = DateTime.UtcNow.AddDays(-1).AddHours(12), EndTime = DateTime.UtcNow.AddDays(-1).AddHours(13), Location = "Blueberry Hill", OrganizerUserId = salesStaff.Id, Status = "Completed" }
                        );
                    }
                    await context.SaveChangesAsync();
                }


                // Seed Customer Data (Orders & Tickets)
                var customerUser2 = await userManager.FindByEmailAsync("customer@clientsphere.com");
                if (customerUser2 != null)
                {
                    // Ensure Customer record exists for this user to link Orders
                    var customerRecord = await context.Customers.FirstOrDefaultAsync(c => c.Email == customerUser2.Email);
                    if (customerRecord == null)
                    {
                        customerRecord = new Customer
                        {
                            ContactName = (customerUser2.FirstName ?? "John") + " " + (customerUser2.LastName ?? "Doe"),
                            Email = customerUser2.Email,
                            Phone = customerUser2.PhoneNumber ?? "555-0123",
                            CompanyName = "Acme Corp",
                            CreatedAt = DateTime.UtcNow
                        };
                        context.Customers.Add(customerRecord);
                        await context.SaveChangesAsync();
                    }

                    if (!context.Orders.Any(o => o.CustomerId == customerRecord.Id))
                    {
                        var products = context.Products.ToList();
                        if (!products.Any())
                        {
                             context.Products.AddRange(
                                new Product { Name = "Widget A", Price = 600 },
                                new Product { Name = "Gadget B", Price = 850.50m },
                                new Product { Name = "Tool C", Price = 816.50m }
                             );
                             await context.SaveChangesAsync();
                             products = context.Products.ToList();
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

                    if (!context.SupportTickets.Any(t => t.CustomerId == customerUser2.Id))
                    {
                        context.SupportTickets.AddRange(
                            new SupportTicket { Subject = "Billing inquiry - Invoice #INV-2024-001", Description = "I have a question about the tax amount.", Status = "Resolved", Priority = "Low", CustomerId = customerUser2.Id, CreatedAt = DateTime.UtcNow.AddDays(-5), LastUpdated = DateTime.UtcNow.AddDays(-4) },
                            new SupportTicket { Subject = "Feature Request: Export Data", Description = "It would be great to export my orders to CSV.", Status = "In Progress", Priority = "Medium", CustomerId = customerUser2.Id, CreatedAt = DateTime.UtcNow.AddDays(-1), LastUpdated = DateTime.UtcNow }
                        );
                        await context.SaveChangesAsync();
                    }
                }

                // Seed Marketing Staff Data (Campaigns)
                var marketingStaff = await userManager.FindByEmailAsync("marketing.staff@clientsphere.com");
                if (marketingStaff != null)
                {
                    if (!context.Campaigns.Any(c => c.ManagedByUserId == marketingStaff.Id))
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

                // Seed Billing Data (Invoices)
                try
                {
                    var customerForBilling = await context.Customers.FirstOrDefaultAsync(c => c.Email == "customer@clientsphere.com");
                    if (customerForBilling != null)
                    {
                        if (!context.Invoices.Any())
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
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error seeding invoices: {ex.Message}");
                }

            }
        }

        private static async Task UpdateUserPasswords(UserManager<ApplicationUser> userManager)
        {
            // Map of email -> correct password as per ACCOUNTS.txt
            var correctPasswords = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "superadmin@clientsphere.com", "SuperAdmin123!" },
                { "admin@clientsphere.com",      "Admin123!" },
                { "sales.manager@clientsphere.com",    "Sales123!" },
                { "sales.staff@clientsphere.com",      "Staff123!" },
                { "marketing.manager@clientsphere.com","Marketing123!" },
                { "marketing.staff@clientsphere.com",  "Marketing123!" },
                { "support.staff@clientsphere.com",    "Support123!" },
                { "billing.staff@clientsphere.com",    "Billing123!" },
                { "customer@clientsphere.com",         "Customer123!" },
            };

            var users = userManager.Users.ToList();
            foreach (var user in users)
            {
                if (user.Email == null) continue;

                if (correctPasswords.TryGetValue(user.Email, out var correctPassword))
                {
                    // Check if current password is already correct
                    var passwordOk = await userManager.CheckPasswordAsync(user, correctPassword);
                    if (!passwordOk)
                    {
                        // Reset to the correct password
                        var token = await userManager.GeneratePasswordResetTokenAsync(user);
                        var result = await userManager.ResetPasswordAsync(user, token, correctPassword);
                        if (result.Succeeded)
                            Console.WriteLine($"[DbInitializer] Password restored for {user.Email}");
                        else
                            Console.WriteLine($"[DbInitializer] Failed to restore password for {user.Email}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                    }
                }
            }
        }
    }
}
