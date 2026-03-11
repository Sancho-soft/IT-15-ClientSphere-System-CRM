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

    // ─── Marketing Manager (team-level) ────────────────────────────────
    public class MarketingManagerDashboardViewModel
    {
        public int TotalCampaigns { get; set; }
        public int ActiveCampaigns { get; set; }
        public int CompletedCampaigns { get; set; }
        public int TotalRecipients { get; set; }
        public int TotalConversions { get; set; }
        public double AvgResponseRate { get; set; }
        public decimal TotalBudget { get; set; }
        public int TeamMemberCount { get; set; }
        public List<CampaignTypeStats> CampaignsByType { get; set; } = new();
        public List<CampaignStaffViewModel> RecentCampaigns { get; set; } = new();
    }

    public class CampaignTypeStats
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public decimal TotalBudget { get; set; }
        public int Conversions { get; set; }
    }
}
