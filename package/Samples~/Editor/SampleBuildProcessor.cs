using System;
using System.IO;
using System.Linq;

using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;

namespace Samples
{
    /// <summary>
    /// Simple build processor that makes sure that any custom configuration that the user creates is
    /// correctly passed on to the provider implementation at runtime.
    ///
    /// Custom configuration instances that are stored in EditorBuildSettings are not copied to the target build
    /// as they are considered unreferenced assets. In order to get them to the runtime side of things, they need
    /// to be serialized to the build app and deserialized at runtime. Previously this would be a manual process
    /// requiring the implementor to manually serialize to some location that can then be read from to deserialize
    /// at runtime. With thenew PlayerSettings Prelaoded Assets API we can now just add our asset to the prelaoded
    /// list and have it be instantiated at app launch.
    ///
    /// Note that the preloaded assets are only notified with Awake, so anything you want or need to do with the
    /// asset after launch needs to be handled there.
    ///
    /// More info on APIs used here:
    /// * <a href="https://docs.unity3d.com/ScriptReference/EditorBuildSettings.html">EditorBuildSettings</a>
    /// * <a href="https://docs.unity3d.com/ScriptReference/PlayerSettings.GetPreloadedAssets.html">PlayerSettings.GetPrelaodedAssets</a>
    /// * <a href="https://docs.unity3d.com/ScriptReference/PlayerSettings.SetPreloadedAssets.html">PlayerSettings.SetPrelaodedAssets</a>
    /// </summary>
    public class SampleBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport
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
                where s != null && s.GetType() == typeof(SampleSettings)
                select s;

            if (oldSettings != null && oldSettings.Any())
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

            SampleSettings settings = null;
            EditorBuildSettings.TryGetConfigObject(SampleConstants.k_SettingsKey, out settings);
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
