using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class CustomerDashboardViewModel
    {
        public int TotalOrders { get; set; }
        public int ActiveTickets { get; set; }
        public decimal TotalSpent { get; set; }
        public List<CustomerOrderViewModel> RecentOrders { get; set; } = new List<CustomerOrderViewModel>();
        public List<TicketViewModel> RecentTickets { get; set; } = new List<TicketViewModel>();
    }

    public class CustomerOrderViewModel
    {
        public string OrderId { get; set; }
        public DateTime OrderDate { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public int ItemCount { get; set; }
    }
}
