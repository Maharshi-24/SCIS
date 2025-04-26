namespace SCIS
{
    public static class Constants
    {
        public static class UserTypes
        {
            public const string SystemAdmin = "SystemAdmin";
            public const string ContentAdmin = "ContentAdmin";
            public const string ClubManager = "ClubManager";
            public const string User = "User";
            public const string Admin = "Admin"; // For backward compatibility
        }

        public static class UserRoles
        {
            public const string SystemAdmin = "SystemAdmin";
            public const string ContentAdmin = "ContentAdmin";
            public const string ClubPresident = "ClubPresident";
            public const string ClubSecretary = "ClubSecretary";
            public const string ClubTreasurer = "ClubTreasurer";
            public const string Member = "Member";

            // For backward compatibility
            public const string Student = "Student";
            public const string Faculty = "Faculty";
            public const string Staff = "Staff";
            public const string Alumni = "Alumni";
        }

        public static class MembershipStatus
        {
            public const string Pending = "Pending";
            public const string Approved = "Approved";
            public const string Rejected = "Rejected";
        }

        public static class AnnouncementStatus
        {
            public const string Active = "Active";
            public const string Inactive = "Inactive";
        }
    }
}
