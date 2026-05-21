namespace ClientSphere.Constants
{
    /// <summary>
    /// Centralized role name constants. Eliminates typo-prone string duplication across controllers and filters.
    /// </summary>
    public static class Roles
    {
        public const string SuperAdmin = "Super Admin";
        public const string Admin = "Admin";
        public const string SalesManager = "Sales Manager";
        public const string SalesStaff = "Sales Staff";
        public const string MarketingManager = "Marketing Manager";
        public const string MarketingStaff = "Marketing Staff";
        public const string SupportStaff = "Support Staff";
        public const string BillingStaff = "Billing Staff";
        public const string Customer = "Customer";

        /// <summary>All roles used by DbInitializer for seeding.</summary>
        public static readonly string[] All = {
            SuperAdmin, Admin, SalesManager, SalesStaff,
            MarketingManager, MarketingStaff, SupportStaff,
            BillingStaff, Customer
        };
    }

    /// <summary>
    /// Centralized TempData key constants used across controllers and views.
    /// </summary>
    public static class TempDataKeys
    {
        public const string SuccessMessage = "SuccessMessage";
        public const string ErrorMessage = "ErrorMessage";
        public const string CurrentPage = "CurrentPage";
        public const string ToastMessage = "ToastMessage";
        public const string ToastType = "ToastType";
        public const string Success = "Success";
    }

    /// <summary>
    /// Common status display strings.
    /// </summary>
    public static class StatusValues
    {
        public const string Active = "Active";
        public const string Inactive = "Inactive";
        public const string NoRole = "No Role";
        public const string Unknown = "Unknown";
    }
}
