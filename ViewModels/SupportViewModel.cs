using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class SupportDashboardViewModel
    {
        public int TotalTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int ResolvedTickets { get; set; }
        public int CriticalTickets { get; set; }
        public List<TicketViewModel> Tickets { get; set; } = new List<TicketViewModel>();
    }

    public class TicketViewModel
    {
        public int Id { get; set; } // Database ID
        public string TicketId { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Open"; // Open, In Progress, Resolved, Closed
        public string Priority { get; set; } = "Low"; // Low, Medium, High, Critical
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string AssignedTo { get; set; } = "Unassigned";
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
