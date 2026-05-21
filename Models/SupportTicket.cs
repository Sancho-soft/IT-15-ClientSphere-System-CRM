using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class SupportTicket
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Subject")]
        [StringLength(200, ErrorMessage = "Subject cannot exceed 200 characters.")]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        [StringLength(2000, ErrorMessage = "Description cannot exceed 2000 characters.")]
        public string Description { get; set; }

        public string Status { get; set; } // Open, In Progress, Resolved, Closed
        
        public string Priority { get; set; } // Low, Medium, High, Critical

        public string CustomerId { get; set; } // Link to Customer User
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastUpdated { get; set; }

        public string? ImageUrl { get; set; } // Cloudinary URL
    }
}
