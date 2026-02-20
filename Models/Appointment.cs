using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientSphere.Models
{
    public class Appointment
    {
        public int Id { get; set; }

        [Required]
        public string Title { get; set; }

        public string Description { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        public string Location { get; set; }

        public int? CustomerId { get; set; } // Optional link to customer
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        public string OrganizerUserId { get; set; } // The Sales Staff

        public string Status { get; set; } = "Scheduled"; // Scheduled, Completed, Cancelled
        
        public string? ExternalCalendarId { get; set; } // Microsoft Graph Event ID

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
