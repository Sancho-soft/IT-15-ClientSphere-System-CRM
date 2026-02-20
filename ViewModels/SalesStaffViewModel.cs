using System;

namespace ClientSphere.ViewModels
{
    public class SalesStaffViewModel
    {
        public decimal MyRevenueToday { get; set; }
        public decimal MyRevenueMonth { get; set; }
        public int MyDealsClosed { get; set; }
        public int MyPendingLeads { get; set; }
        public IEnumerable<SalesItemViewModel> MyRecentSales { get; set; } = new List<SalesItemViewModel>();
    }
}
