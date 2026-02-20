using System;
using System.Collections.Generic;

namespace ClientSphere.ViewModels
{
    public class MarketingDashboardViewModel
    {
        public int TotalCampaigns { get; set; }
        public decimal ActiveBudget { get; set; }
        public int TotalRecipients { get; set; }
        public double AvgResponseRate { get; set; }
        public List<CampaignViewModel> Campaigns { get; set; } = new List<CampaignViewModel>();
    }

    public class CampaignViewModel
    {
        public int Id { get; set; }
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "Email"; // Email, SMS, Social Media, Call
        public string Status { get; set; } = "Draft"; // Draft, Active, Completed, Paused
        public decimal Budget { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int Recipients { get; set; }
        public int Responses { get; set; }
        public double ResponseRate { get; set; }
        public string ManagedBy { get; set; } = string.Empty;
    }
}
