using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class MarketingStaffDashboardViewModel
    {
        public int TotalCampaigns { get; set; }
        // Budget removed for Staff
        public int TotalRecipients { get; set; }
        public double AvgResponseRate { get; set; }
        public List<CampaignStaffViewModel> Campaigns { get; set; } = new List<CampaignStaffViewModel>();
    }

    public class CampaignStaffViewModel
    {
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Email";
        public string Status { get; set; } = "Draft";
        // Budget removed for Staff
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Recipients { get; set; }
        public int Responses { get; set; }
        public double ResponseRate { get; set; }
        public string ManagedBy { get; set; } = string.Empty;
    }
}
