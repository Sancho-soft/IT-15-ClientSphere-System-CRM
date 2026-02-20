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
        public decimal EstimatedValue { get; set; }

        public string Stage { get; set; } // Prospecting, Qualification, Proposal, Negotiation, Closed Won, Closed Lost
        
        [Display(Name = "Probability (%)")]
        public int Probability { get; set; }

        [Display(Name = "Expected Close Date")]
        [DataType(DataType.Date)]
        public DateTime ExpectedCloseDate { get; set; }

        public string AssignedToUserId { get; set; } // Link to Sales Staff
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
