using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientSphere.Models
{
    public class Opportunity
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } // e.g., "500 Unit Order for Acme Corp"

        [Display(Name = "Expected Value")]
        [Column(TypeName = "decimal(18, 2)")]
        [Range(0, double.MaxValue, ErrorMessage = "Estimated value must be 0 or greater.")]
        public decimal EstimatedValue { get; set; }

        public string? Stage { get; set; } // Prospecting, Qualification, Proposal, Negotiation, Closed Won, Closed Lost
        
        [Display(Name = "Probability (%)")]
        [Range(0, 100, ErrorMessage = "Probability must be between 0 and 100.")]
        public int Probability { get; set; }

        [Display(Name = "Expected Close Date")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; }

        public string? AssignedToUserId { get; set; } // Link to Sales Staff
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
