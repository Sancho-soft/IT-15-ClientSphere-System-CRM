using System;

namespace ClientSphere.ViewModels
{
    public class UserManagementViewModel
    {
        public List<UserItemViewModel> Users { get; set; } = new List<UserItemViewModel>();
        public int TotalUsers { get; set; }
        public int AdminCount { get; set; }
        public int SalesTeamCount { get; set; }
        public int SupportTeamCount { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class UserItemViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Initials { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public DateTime? LastActive { get; set; }
        public string LastActiveDisplay { get; set; } = "Never";
    }
}
