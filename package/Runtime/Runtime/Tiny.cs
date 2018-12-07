

namespace Unity.Tiny
{
    internal static class TinyConstants
    {
        private const string Sep = "/";

        public const string ApplicationName = "Tiny";
        public const string PackageName = "com.unity.tiny";
        public const string PackagePath = "Packages" + Sep + PackageName;

        internal static class MenuItemNames
        {
            private const string Edit = "Edit";
            public const string DuplicateSelection     = ApplicationName + Sep + Edit + Sep + "Duplicate Selection %#D";
            public const string DeleteSelection        = ApplicationName + Sep + Edit + Sep + "Delete Selection";

            private const string Help = "Help";
            public const string BugReportWindow        = ApplicationName + Sep + Help + Sep + "Report a Bug...";
        }
    }
}

