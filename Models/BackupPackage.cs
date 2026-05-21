using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace ClientSphere.Models
{
    public class BackupPackage
    {
        public List<ApplicationUser> Users { get; set; } = new();
        public List<IdentityRole> Roles { get; set; } = new();
        public List<IdentityUserRole<string>> UserRoles { get; set; } = new();
        public List<Customer> Customers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
        public List<Order> Orders { get; set; } = new();
        public List<OrderItem> OrderItems { get; set; } = new();
        public List<Lead> Leads { get; set; } = new();
        public List<Opportunity> Opportunities { get; set; } = new();
        public List<Appointment> Appointments { get; set; } = new();
        public List<SupportTicket> SupportTickets { get; set; } = new();
        public List<Campaign> Campaigns { get; set; } = new();
        public List<Invoice> Invoices { get; set; } = new();
        public List<PaymentRecord> PaymentRecords { get; set; } = new();
        public List<AuditLog> AuditLogs { get; set; } = new();
        public List<SystemSetting> SystemSettings { get; set; } = new();
        public List<Notification> Notifications { get; set; } = new();
    }
}
