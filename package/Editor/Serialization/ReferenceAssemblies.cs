#if (NET_4_6 || NET_STANDARD_2_0)

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;

namespace Unity.Properties.Editor.Serialization
{
    public static class ReferenceAssemblies
    {
        // @TODO check https://ono.unity3d.com/unity/unity/pull-request/66347/_/dcc/users-scripts/resolving-user-scripts-bug
        // 
        public static List<Assembly> Assemblies = new List<Assembly>()
        {
            typeof(object).Assembly,
            typeof(Enumerable).Assembly,
            typeof(List<>).Assembly,
            typeof(IPropertyContainer).Assembly,
            typeof(UnityEngine.Object).Assembly,
        };

        public static List<string> Locations = Assemblies.Select(assembly => assembly.Location).ToList();
    }
}

#endif // (NET_4_6 || NET_STANDARD_2_0)
