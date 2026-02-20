using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        public string Description { get; set; }

        public string Status { get; set; } // Open, In Progress, Resolved, Closed
        
        public string Priority { get; set; } // Low, Medium, High, Critical

        public string CustomerId { get; set; } // Link to Customer User
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }
    }
}
