using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class Lead
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        public string Phone { get; set; }
        public string Company { get; set; }
        
        public string Source { get; set; } // e.g., Website, Referral, Cold Call
        
        public string Status { get; set; } // New, Contacted, Qualified, Lost

        [Display(Name = "Assigned To")]
        public string AssignedToUserId { get; set; } // Link to Sales Staff
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
