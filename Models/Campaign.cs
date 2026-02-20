using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientSphere.Models
{
    public class Campaign
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public string Type { get; set; } // Email, SMS, Social Media, etc.

        public string Status { get; set; } // Draft, Active, Completed, Cancelled

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public decimal Budget { get; set; }
        public decimal ExpectedRevenue { get; set; }

        public int TargetAudienceSize { get; set; }
        public int ActualRecipients { get; set; }
        
        // Metrics
        public int Opens { get; set; }
        public int Clicks { get; set; }
        public int Conversions { get; set; }

        public string ManagedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
