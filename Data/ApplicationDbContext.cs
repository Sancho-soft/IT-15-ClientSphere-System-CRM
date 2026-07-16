using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using ClientSphere.Models;

namespace ClientSphere.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        private readonly IDataProtector? _protector;

        public ApplicationDbContext(
            DbContextOptions<ApplicationDbContext> options,
            IDataProtectionProvider? dataProtectionProvider = null)
            : base(options)
        {
            if (dataProtectionProvider != null)
            {
                _protector = dataProtectionProvider.CreateProtector("ClientSphere.PII.Encryption.v1");
            }
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            // Customize the ASP.NET Identity model and override the defaults if needed.
            // For example, you can rename the ASP.NET Identity table names and more.
            // Add your customizations after calling base.OnModelCreating(builder);

            if (_protector != null)
            {
                var piiConverter = new ValueConverter<string, string>(
                    v => string.IsNullOrEmpty(v) ? v : _protector.Protect(v),
                    v => string.IsNullOrEmpty(v) ? v : _protector.Unprotect(v)
                );

                builder.Entity<Customer>()
                    .Property(c => c.Address)
                    .HasMaxLength(2000)
                    .HasConversion(piiConverter);
            }
            else
            {
                builder.Entity<Customer>()
                    .Property(c => c.Address)
                    .HasMaxLength(2000);
            }

            builder.Entity<Campaign>()
                .Property(c => c.Budget)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Campaign>()
                .Property(c => c.ExpectedRevenue)
                .HasColumnType("decimal(18,2)");
        }

        public DbSet<Customer> Customers { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Lead> Leads { get; set; }
        public DbSet<Opportunity> Opportunities { get; set; }
        public DbSet<SupportTicket> SupportTickets { get; set; }
        public DbSet<Campaign> Campaigns { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<PaymentRecord> PaymentRecords { get; set; }
        public DbSet<BackupHistory> BackupHistories { get; set; }
    }
}
