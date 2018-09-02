using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
namespace ResourceManagement.Util
{
    public static class Config
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

        static Dictionary<string, string> cachedValues = new Dictionary<string, string>();

        public static string GetGlobalVar(string var)
        {
            int i = var.LastIndexOf('.');
            if (i < 0)
                return var;

            string cachedValue = null;
            if (cachedValues.TryGetValue(var, out cachedValue))
                return cachedValue;

            var className = var.Substring(0, i);
            var propName = var.Substring(i + 1);
            foreach (var a in GetAssemblies())
            {
                Type t = a.GetType(className, false, false);
                if (t == null)
                    continue;
                try
                {
                    var pi = t.GetProperty(propName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public);
                    if (pi != null)
                    {
                        var v = pi.GetValue(null, null);
                        if (v != null)
                        {
                            cachedValues.Add(var, v.ToString());
                            return v.ToString();
                        }
                    }
                    var fi = t.GetField(propName, System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.FlattenHierarchy | System.Reflection.BindingFlags.Public);
                    if (fi != null)
                    {
                        var v = fi.GetValue(null);
                        if (v != null)
                        {
                            cachedValues.Add(var, v.ToString());
                            return v.ToString();
                        }
                    }
                }
                catch (Exception)
                {
                }
            }
            return var;
        }

        static Dictionary<string, string> cachedPaths = new Dictionary<string, string>();
        public static string ExpandPathWithGlobalVars(string p)
        {
            string val = null;
            if (!cachedPaths.TryGetValue(p, out val))
                cachedPaths.Add(p, val = ExpandWithVars(p, '{', '}', GetGlobalVar));
            return val;
        }

        public static string ExpandWithVars(string p, char startDelim, char endDelim, Func<string, string> varFunc)
        {
            while (true)
            {
                int i = p.IndexOf(startDelim);
                if (i < 0)
                    return p;
                int e = p.IndexOf(endDelim, i);
                if (e < i)
                    return p;
                var token = p.Substring(i + 1, e - i - 1);
                var tokenVal = varFunc(token);
                p = p.Substring(0, i) + tokenVal + p.Substring(e + 1);
            }
        }


        public static bool IsInstance<A, B>()
        {
            var tA = typeof(A);
            var tB = typeof(B);
#if !UNITY_EDITOR && UNITY_WSA_10_0 && ENABLE_DOTNET
            return tB.GetTypeInfo().IsAssignableFrom(tA.GetTypeInfo());
#else
            return tB.IsAssignableFrom(tA);
#endif
        }

    }
}
