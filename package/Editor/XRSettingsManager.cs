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
    class XRSettingsManager : SettingsProvider
    {
        static XRSettingsManager s_SettingsManager = null;

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
                    string settingsPath = String.Format("XR/{0}", xrbda.displayName);
                    var resProv = new XRConfigurationProvider(settingsPath, xrbda.displayName, xrbda.buildSettingsKey, at);
                    ret.Add(resProv);
                }
            }

            return ret.ToArray();
        }
    }
}
