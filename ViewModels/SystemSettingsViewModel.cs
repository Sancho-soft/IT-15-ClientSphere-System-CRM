using System.ComponentModel.DataAnnotations;

namespace ClientSphere.ViewModels
{
    public class SystemSettingsViewModel
    {
        // General
        [Required]
        [Range(1, 1440)]
        [Display(Name = "Session Timeout (minutes)")]
        public int SessionTimeoutMinutes { get; set; } = 15;

        [Required]
        [Range(6, 128)]
        [Display(Name = "Minimum Password Length")]
        public int MinimumPasswordLength { get; set; } = 8;

        // Email Settings
        [Required]
        [Display(Name = "SMTP Server")]
        public string SmtpServer { get; set; } = string.Empty;

        [Required]
        [Range(1, 65535)]
        [Display(Name = "SMTP Port")]
        public int SmtpPort { get; set; } = 587;

        [Required]
        [Display(Name = "Encryption")]
        public string SmtpEncryption { get; set; } = "TLS";

        [Required]
        [EmailAddress]
        [Display(Name = "From Email Address")]
        public string FromEmailAddress { get; set; } = string.Empty;

        // Notification Settings
        [Display(Name = "New User Registrations")]
        public bool NotifyNewRegistrations { get; set; } = true;

        [Display(Name = "System Updates")]
        public bool NotifySystemUpdates { get; set; } = true;

        [Display(Name = "Critical Alerts")]
        public bool NotifyCriticalAlerts { get; set; } = true;

        // Database & Backup
        [Display(Name = "Automatic Backups")]
        public bool AutomaticBackups { get; set; } = true;

        [Display(Name = "Backup Frequency")]
        public string BackupFrequency { get; set; } = "Daily";

        [Display(Name = "Retention Period")]
        public string RetentionPeriod { get; set; } = "30 days";

        // API & Integrations
        [Display(Name = "API Access")]
        public bool ApiAccessEnabled { get; set; } = true;

        [Range(1, 10000)]
        [Display(Name = "Rate Limit (requests per minute)")]
        public int ApiRateLimit { get; set; } = 100;

        [Display(Name = "System API Key")]
        public string SystemApiKey { get; set; } = string.Empty;
    }
}
