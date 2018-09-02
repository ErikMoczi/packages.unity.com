using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.PackageManager.UI
{
    internal static class PackageInfoListExtensions 
    {
        public static PackageInfo ById(this IEnumerable<PackageInfo> list, string id)
        {
            return (from package in list where package.PackageId == id select package).FirstOrDefault();
        }
        
        public static IEnumerable<PackageInfo> ByName(this IEnumerable<PackageInfo> list, string name)
        {
            return from package in list where package.Name == name select package;
        }

        public static void SetCurrent(this IEnumerable<PackageInfo> list, bool current = true)
        {
            foreach (var package in list)
            {
                package.IsCurrent = current;
            }
        }

        public static bool ContainsPackage(this IEnumerable<PackageInfo> list, string id)
        {
            return list.ById(id) != null;
        }

        public static void SetGroup(this IEnumerable<PackageInfo> list, string group)
        {
            foreach (var package in list)
            {
                package.Group = group;
            }
        }
    }
}
