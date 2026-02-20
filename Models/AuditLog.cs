using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class AuditLog
    {
        public int Id { get; set; }

        [Required]
        public string Action { get; set; } // e.g., "User Login", "Create Order"

        public string Description { get; set; } // e.g., "User sales.staff logged in"

        public string UserId { get; set; } // ID of the user performing the action

        public string UserName { get; set; } // Snapshot of username for easy display

        public string IpAddress { get; set; }

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
