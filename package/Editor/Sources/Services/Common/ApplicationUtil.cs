using System.Linq;
using UnityEngine;

namespace UnityEditor.PackageManager.UI
{
    class ApplicationUtil
    {
        public static string SelectedClassName = "selected";

        public static string ResetPackagesMenuName = "Reset Packages to defaults";
        public static string ResetPackagesMenuPath = "Help/" + ResetPackagesMenuName;

        public static bool IsPreReleaseVersion
        {
            get
            {
                var lastToken = Application.unityVersion.Split('.').LastOrDefault();
                return lastToken.Contains("a") || lastToken.Contains("b");
            }
        }
    }
}
