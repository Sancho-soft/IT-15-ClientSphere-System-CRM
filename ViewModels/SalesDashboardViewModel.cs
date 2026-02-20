using ClientSphere.Models;

namespace ClientSphere.ViewModels
{
    public class SalesDashboardViewModel
    {
        public decimal TotalRevenue { get; set; }
        public decimal PendingRevenue { get; set; }
        public int TotalSalesCount { get; set; }
        public decimal AvgDealSize { get; set; }
        public IEnumerable<Order> RecentSales { get; set; } = new List<Order>();
    }
}
