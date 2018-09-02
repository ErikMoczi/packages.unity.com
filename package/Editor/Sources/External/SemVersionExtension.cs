namespace Semver
{
    internal static class SemVersionExtension
    {
        public static string VersionOnly(this SemVersion version)
        {
            return "" + version.Major + "." + version.Minor + "." + version.Patch;
        }
        
        public static string ShortVersion(this SemVersion version)
        {
            var versionStr = "" + version.Major + "." + version.Minor;
            if (!string.IsNullOrEmpty(version.Prerelease))
                versionStr += "-" + version.Prerelease;
            if (!string.IsNullOrEmpty(version.Build))
                versionStr += "+" + version.Build;
            return versionStr;
        }                
    }
}