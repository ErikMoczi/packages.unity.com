

using System;
using System.Reflection;

using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    internal static class TinyDomain
    {
        private static bool s_Loaded;

        #if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void Init()
        {
            UnityEditor.AssemblyReloadEvents.afterAssemblyReload += HandleAssemblyReloaded;
        }
        #endif

        private static void HandleAssemblyReloaded()
        {
            // This check is intentional so that we don't allocate the string every time this method is called.
            if (!s_Loaded)
            {
                Assert.IsFalse(s_Loaded, GetAssemblyReloadErrorString());
            }
        }
        
        // This method exists only to lazily compute the error string, when needed.
        private static string GetAssemblyReloadErrorString()
        {
            return  $"{TinyConstants.ApplicationName}: A {TinyConstants.ApplicationName} project should not be loaded during assembly reload";
        }

        internal static void LoadDomain()
        {
            if (!s_Loaded)
            {
                ProcessInitializeOnLoad();
            }
            s_Loaded = true;
        }

        private static void ProcessInitializeOnLoad()
        {
            foreach (var onLoad in TinyAttributeScanner.GetTypeAttributes<TinyInitializeOnLoad>())
            {
                if (!ValidateType(onLoad.Type))
                {
                    continue;
                }

                // Call static constructor
                System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(onLoad.Type.TypeHandle);
            }

            foreach (var onLoadMethod in TinyAttributeScanner.GetMethodAttributes<TinyInitializeOnLoad>())
            {
                if (!ValidateMethod(onLoadMethod.Method))
                {
                    continue;
                }

                onLoadMethod.Method.Invoke(null, null);
            }
        }

        private static bool ValidateType(Type type)
        {
            return  type.IsAbstract
                &&  type.IsSealed
                && !type.IsGenericType;
        }

        private static bool ValidateMethod(MethodInfo method)
        {
            return  method.IsStatic
                &&  method.GetParameters().Length == 0
                &&  method.ReturnType == typeof(void)
                && !method.IsGenericMethod;
        }
    }
}

