using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEditor;

using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    internal static class TypeLoaderExtensions
    {
        public static IEnumerable<Type> GetLoadableTypes(this Assembly assembly)
        {
            if (assembly == null) throw new ArgumentNullException("assembly");
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null);
            }
        }

        public static IEnumerable<Type> GetTypesWithInterface<T>(this Assembly asm)
        {
            var it = typeof(T);
            return from lt in asm.GetLoadableTypes()
                where it.IsAssignableFrom(lt) && !lt.IsAbstract && !lt.IsInterface
                select lt;
        }

        public static IEnumerable<Type> GetAllTypesWithInterface<T>()
        {
            IEnumerable<Type> ret = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var vendorTypes = asm.GetTypesWithInterface<T>().ToList<Type>();
                ret = ret.Concat(vendorTypes);
            }

            return ret;
        }


        public static IEnumerable<Type> GetTypesWithAttribute<T>(this Assembly asm)
        {
            var it = typeof(T);
            return from lt in asm.GetLoadableTypes()
                where (lt.GetCustomAttributes(it, true).Length > 0) && !lt.IsAbstract && !lt.IsInterface
                select lt;
        }


        public static IEnumerable<Type> GetAllTypesWithAttribute<T>()
        {
            IEnumerable<Type> ret = new List<Type>();

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var vendorTypes = asm.GetTypesWithAttribute<T>().ToList<Type>();
                ret = ret.Concat(vendorTypes);
            }

            return ret;
        }

    }

    class XRSettingsManager : SettingsProvider {

        private static XRSettingsManager s_SettingsManager = null;

        [UnityEngine.Internal.ExcludeFromDocs]
        XRSettingsManager(string path, SettingsScopes scopes = SettingsScopes.Any) : base(path, scopes)
        {

        }

        [SettingsProvider]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider Create()
        {
            if (s_SettingsManager == null)
            {
                s_SettingsManager = new XRSettingsManager("XR");
            }

            return s_SettingsManager;
        }

        [SettingsProviderGroup]
        [UnityEngine.Internal.ExcludeFromDocs]
        static SettingsProvider[] CreateAllChildSettingsProviders()
        {
            List<SettingsProvider> ret = new List<SettingsProvider>();
            if (s_SettingsManager != null)
            {
                var ats = TypeLoaderExtensions.GetAllTypesWithAttribute<XRConfigurationDataAttribute>();
                foreach (var at in ats)
                {
                    XRConfigurationDataAttribute xrbda = at.GetCustomAttributes(typeof(XRConfigurationDataAttribute), true)[0] as XRConfigurationDataAttribute;
                    string settingsPath = String.Format("XR/{0}", xrbda.DisplayName);
                    var resProv = new XRConfigurationProvider(settingsPath, xrbda.DisplayName, xrbda.BuildSettingsKey, at);
                    ret.Add(resProv);
                }
            }

            return ret.ToArray();
        }
    }
}
