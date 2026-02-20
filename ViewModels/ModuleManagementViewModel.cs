using System;

namespace ClientSphere.ViewModels
{
    public class ModuleManagementViewModel
    {
        public List<ModuleItemViewModel> Modules { get; set; } = new List<ModuleItemViewModel>();
    }

    public class ModuleItemViewModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = "Active";
        public int ActiveUsers { get; set; }
        public int TotalRecords { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
