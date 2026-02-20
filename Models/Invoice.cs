using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ClientSphere.Models
{
    public class Invoice
    {
        public int Id { get; set; }

        [Required]
        public string InvoiceNumber { get; set; } // e.g. INV-2024-001

        public int CustomerId { get; set; }
        [ForeignKey("CustomerId")]
        public Customer Customer { get; set; }

        public int OrderId { get; set; } // Optional link to an Order
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public DateTime IssueDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? PaidDate { get; set; }

        public string Status { get; set; } // Draft, Sent, Paid, Overdue, Cancelled
        
        public string PaymentMethod { get; set; } // Credit Card, Bank Transfer, PayPal

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
