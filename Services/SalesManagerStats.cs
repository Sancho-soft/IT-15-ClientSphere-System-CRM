using System.Collections.Generic;

namespace ClientSphere.Services
{
    public class SalesManagerStats
    {
        public decimal TotalRevenueMTD { get; set; }
        public double RevenueGrowth { get; set; }
        public decimal QuotaAchievement { get; set; }
        public int DealsClosedMTD { get; set; }
        public double DealsGrowth { get; set; }
        public decimal AvgDealSize { get; set; }
        public List<Models.Order> PendingHighValueDeals { get; set; }
    }
}
