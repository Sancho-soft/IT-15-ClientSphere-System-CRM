using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty; // Target user (or role-based virtual ID)

        public string? ForRole { get; set; } // If set, visible to all users in this role

        [Required]
        public string Title { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        public string? Url { get; set; } // Optional navigation link

        public string? Category { get; set; } // "Billing", "Support", "Sales", etc.

        public bool IsRead { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
