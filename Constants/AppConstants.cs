namespace MyPhotoBiz.Constants
{
    public static class AppConstants
    {
        public static class Roles
        {
            public const string Admin = "Admin";
            public const string Client = "Client";
            public const string Photographer = "Photographer";
        }

        public static class Pagination
        {
            public const int DefaultPageSize = 20;
            public const int MaxPageSize = 100;
        }

        public static class InvoiceSettings
        {
            public const int InvoiceNumberMinRange = 1000;
            public const int InvoiceNumberMaxRange = 9999;
        }

        public static class Security
        {
            public const int MinPasswordLength = 12;
            public const int MaxLoginAttempts = 5;
            public const int LockoutMinutes = 15;
        }
    }
}
