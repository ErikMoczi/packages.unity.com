using System;
using System.Reflection;

namespace UnityEngine.ResourceManagement
{
    public static class ResourceManagerConfig
    {
        public static bool IsInstance<T1, T2>()
        {
            var tA = typeof(T1);
            var tB = typeof(T2);
#if !UNITY_EDITOR && UNITY_WSA_10_0 && ENABLE_DOTNET
            return tB.GetTypeInfo().IsAssignableFrom(tA.GetTypeInfo());
#else
            return tB.IsAssignableFrom(tA);
#endif
        }

    }
}
