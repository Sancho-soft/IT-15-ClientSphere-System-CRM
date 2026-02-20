using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class AnalyticsViewModel
    {
        public decimal TotalRevenue { get; set; }
        public int ActiveUsers { get; set; }
        public double ConversionRate { get; set; }
        public double AvgResponseTimeHours { get; set; }
        
        // Growth percentages
        public double RevenueGrowth { get; set; }
        public double UserGrowth { get; set; }
        public double ConversionGrowth { get; set; }
        public double ResponseTimeImprovement { get; set; }
        
        // Weekly ticket trends
        public Dictionary<string, TicketTrendData> WeeklyTicketTrends { get; set; } = new();
        
        // Monthly lead trends
        public Dictionary<string, int> MonthlyLeadTrends { get; set; } = new();
        
        // Performance summary
        public string TopPerformingModule { get; set; } = string.Empty;
        public int TopModuleUsers { get; set; }
        public string BestConversionModule { get; set; } = string.Empty;
        public double BestConversionRate { get; set; }
        public string FastestResponseModule { get; set; } = string.Empty;
        public double FastestResponseTime { get; set; }
    }

    public class TicketTrendData
    {
        public int Closed { get; set; }
        public int Opened { get; set; }
        public int Pending { get; set; }
    }
}
