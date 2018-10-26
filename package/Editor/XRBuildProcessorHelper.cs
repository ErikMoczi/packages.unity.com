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

namespace UnityEditor.XR.Management
{
    public abstract class XRBuildHelper<T>  : IPreprocessBuildWithReport, IPostprocessBuildWithReport where T : UnityEngine.Object
    {
        public virtual int callbackOrder { get { return 0; } }
        public abstract string BuildSettingsKey { get; }

        void CleanOldSettings()
        {
            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();
            if (preloadedAssets == null)
                return;

            var oldSettings = from s in preloadedAssets
                where s.GetType() == typeof(T)
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

        void SetSettingsForRuntime()
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            UnityEngine.Object settingsObj = null;
            EditorBuildSettings.TryGetConfigObject(BuildSettingsKey, out settingsObj);
            if (settingsObj == null || !(settingsObj is T))
                return;

            UnityEngine.Object[] preloadedAssets = PlayerSettings.GetPreloadedAssets();

            if (!preloadedAssets.Contains(settingsObj))
            {
                var assets = preloadedAssets.ToList();
                assets.Add(settingsObj);
                PlayerSettings.SetPreloadedAssets(assets.ToArray());
            }
        }

        public virtual void OnPreprocessBuild(BuildReport report)
        {
            SetSettingsForRuntime();
        }

        public virtual void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }

    }
}
