using System;
using System.ComponentModel.DataAnnotations;

namespace ClientSphere.Models
{
    public class SystemSetting
    {
        [Key]
        public string Key { get; set; } = string.Empty;
        
        public string Value { get; set; } = string.Empty;
        
        public string Group { get; set; } = "General";
        
        public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
