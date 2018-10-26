using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    class XRGeneralBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        public int callbackOrder
        {
            get { return 0;  }
        }

        void CleanOldSettings()
        {
            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null)
                return;

            var oldSettings = from s in preloadedAssets
                where s.GetType() == typeof(XRGeneralSettings)
                select s;

            if (oldSettings.Any())
            {
                var assets = preloadedAssets.ToList();
                foreach (var s in oldSettings)
                {
                    assets.Remove(s);
                }

                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            XRGeneralSettings settings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out settings);
            if (settings == null)
                return;

            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();

            if (!preloadedAssets.Contains(settings))
            {
                var assets = preloadedAssets.ToList();
                assets.Add(settings);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }

        }

        public void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }
    }
}
