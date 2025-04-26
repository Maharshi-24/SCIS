namespace SCIS.Models
{
    public static class Constants
    {
        // User Types
        public static class UserTypes
        {
            public const string Admin = "Admin";
            public const string ClubManager = "ClubManager";
            public const string User = "User";
        }

        // User Roles
        public static class UserRoles
        {
            // Admin Roles
            public const string SystemAdmin = "SystemAdmin";
            public const string ContentAdmin = "ContentAdmin";
            
            // Club Manager Roles
            public const string ClubPresident = "ClubPresident";
            public const string ClubSecretary = "ClubSecretary";
            public const string ClubTreasurer = "ClubTreasurer";
            
            // User Roles
            public const string Student = "Student";
            public const string Faculty = "Faculty";
            public const string Staff = "Staff";
            public const string Alumni = "Alumni";
        }
    }
}
