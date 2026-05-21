using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class BackupHistory
    {
        [Key]
        public int Id { get; set; }
        
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        public long FileSizeBytes { get; set; }
        
        public string TriggeredByUserId { get; set; } = string.Empty;
        
        public string TriggeredByUserName { get; set; } = string.Empty;
        
        public string Status { get; set; } = "Completed";
    }
}
