using System;
using System.Reflection;
using System.Collections.Generic;

namespace UnityEngine.ResourceManagement
{
    public static class ResourceManagerConfig
    {
#if !UNITY_EDITOR && UNITY_WSA_10_0 && ENABLE_DOTNET
        static Assembly[] GetAssemblies()
        {
            //Not supported on UWP platforms
            return new Assembly[0];
        }
#else
        static Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }
#endif

        static Dictionary<string, string> s_cachedPaths = new Dictionary<string, string>();
        static Dictionary<string, string> s_cachedValues = new Dictionary<string, string>();

        public static void AddCachedValue(string key, string val)
        {
            s_cachedValues.Add(key, val);
        }

        public static string GetGlobalVar(string variableName)
        {
            Debug.Assert(s_cachedValues != null, "ResourceManagerConfig.GetGlobalVar - s_cachedValues == null.");

            if (string.IsNullOrEmpty(variableName))
                return string.Empty;

            string cachedValue = null;
            if (s_cachedValues.TryGetValue(variableName, out cachedValue))
                return cachedValue;

            int i = variableName.LastIndexOf('.');
            if (i < 0)
                return variableName;

            var className = variableName.Substring(0, i);
            var propName = variableName.Substring(i + 1);
            foreach (var a in GetAssemblies())
            {
                Type t = a.GetType(className, false, false);
                if (t == null)
                    continue;
                try
                {
                    var pi = t.GetProperty(propName, BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                    if (pi != null)
                    {
                        var v = pi.GetValue(null, null);
                        if (v != null)
                        {
                            s_cachedValues.Add(variableName, v.ToString());
                            return v.ToString();
                        }
                    }
                    var fi = t.GetField(propName, BindingFlags.Static | BindingFlags.FlattenHierarchy | BindingFlags.Public);
                    if (fi != null)
                    {
                        var v = fi.GetValue(null);
                        if (v != null)
                        {
                            s_cachedValues.Add(variableName, v.ToString());
                            return v.ToString();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return variableName;
        }

        public static string ExpandPathWithGlobalVariables(string inputString)
        {
            Debug.Assert(s_cachedPaths != null, "ResourceManagerConfig.ExpandPathWithGlobalVariables - s_cachedPaths == null.");

            if (string.IsNullOrEmpty(inputString))
                return string.Empty;

            string val = null;
            if (!s_cachedPaths.TryGetValue(inputString, out val))
                s_cachedPaths.Add(inputString, val = ExpandWithVariables(inputString, '{', '}', GetGlobalVar));
            return val;
        }

        public static string ExpandWithVariables(string inputString, char startDelimiter, char endDelimiter, Func<string, string> varFunc)
        {
            if (string.IsNullOrEmpty(inputString))
                return string.Empty;

            while (true)
            {
                int i = inputString.IndexOf(startDelimiter);
                if (i < 0)
                    return inputString;
                int e = inputString.IndexOf(endDelimiter, i);
                if (e < i)
                    return inputString;
                var token = inputString.Substring(i + 1, e - i - 1);
                var tokenVal = varFunc == null ? string.Empty : varFunc(token);
                inputString = inputString.Substring(0, i) + tokenVal + inputString.Substring(e + 1);
            }
        }


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
