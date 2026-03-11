using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientSphere.Models
{
    /// <summary>
    /// Stores a completed payment entry for the Payment History module.
    /// Created when an invoice is confirmed as Paid.
    /// </summary>
    public class PaymentRecord
    {
        public int Id { get; set; }

        public int InvoiceId { get; set; }
        [ForeignKey("InvoiceId")]
        public Invoice? Invoice { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } = string.Empty;

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        public string? PaymentMethod { get; set; } // "PayMongo (GCash)", "PayMongo (Maya)", "Manual", etc.

        public string? TransactionId { get; set; } // PayMongo reference/payment_id

        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountPaid { get; set; }

        public DateTime PaidAt { get; set; } = DateTime.UtcNow;

        public string? Notes { get; set; }
    }
}
