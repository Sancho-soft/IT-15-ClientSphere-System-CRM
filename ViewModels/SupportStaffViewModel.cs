using System;

namespace ClientSphere.ViewModels
{
    public class SupportStaffViewModel
    {
        public int MyOpenTickets { get; set; }
        public int MyResolvedToday { get; set; }
        public int AvgResolutionTimeHours { get; set; }
        public IEnumerable<TicketViewModel> MyActiveTickets { get; set; } = new List<TicketViewModel>();
    }
}
