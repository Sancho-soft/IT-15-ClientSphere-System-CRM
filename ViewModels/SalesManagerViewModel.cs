using ClientSphere.Models;

namespace ClientSphere.ViewModels
{
    public class SalesManagerDashboardViewModel
    {
        // Top Stats
        public decimal TotalRevenueMTD { get; set; }
        public double RevenueGrowth { get; set; } // Percentage
        public decimal QuotaAchievement { get; set; } // Percentage
        public int DealsClosedMTD { get; set; }
        public double DealsGrowth { get; set; } // Percentage
        public decimal AvgDealSize { get; set; }

        // Team Performance
        public List<SalesPersonPerformance> TeamPerformance { get; set; } = new List<SalesPersonPerformance>();

        // High Value Deals (Pending Approval)
        public List<Order> PendingHighValueDeals { get; set; } = new List<Order>();
    }

    public class SalesPersonPerformance
    {
        public int Rank { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = "Sales Staff";
        public decimal TotalSales { get; set; }
        public int DealsCount { get; set; }
        public double Growth { get; set; } // Percentage
    }
}
