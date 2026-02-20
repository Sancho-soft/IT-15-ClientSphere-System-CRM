namespace ClientSphere.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public int ActiveTickets { get; set; }
        public int PendingLeads { get; set; }
        public int ActiveCampaigns { get; set; }
        public int PendingInvoices { get; set; }
        public List<Models.AuditLog> RecentActivities { get; set; } = new List<Models.AuditLog>();
    }
}
