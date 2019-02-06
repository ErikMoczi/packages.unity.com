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

        public virtual UnityEngine.Object SettingsForBuildTargetGroup(BuildTargetGroup buildTargetGroup)
        {
            UnityEngine.Object settingsObj = null;
            EditorBuildSettings.TryGetConfigObject(BuildSettingsKey, out settingsObj);
            if (settingsObj == null || !(settingsObj is T))
                return null;

            return settingsObj;
        }

        void CleanOldSettings()
        {
            BuildHelpers.CleanOldSettings<T>();
        }

        void SetSettingsForRuntime(UnityEngine.Object settingsObj)
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();

            if (!(settingsObj is T))
            {
                Type typeOfT = typeof(T);
                Debug.LogErrorFormat("Settings object is not of type {0}. No settings will be copied to runtime.", typeOfT.Name);
                return;
            }

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
            SetSettingsForRuntime(SettingsForBuildTargetGroup(report.summary.platformGroup));
        }

        public virtual void OnPostprocessBuild(BuildReport report)
        {
            // Always remember to cleanup preloaded assets after build to make sure we don't
            // dirty later builds with assets that may not be needed or are out of date.
            CleanOldSettings();
        }

    }
}
