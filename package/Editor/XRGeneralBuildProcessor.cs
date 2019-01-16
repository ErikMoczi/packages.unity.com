using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;

using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEditor.XR.Management
{
    class XRGeneralBuildProcessor : IPreprocessBuildWithReport, IPostprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        class PreInitInfo
        {
            public PreInitInfo(IXRLoaderPreInit loader, BuildTarget buildTarget, BuildTargetGroup buildTargetGroup)
            {
                this.loader = loader;
                this.buildTarget = buildTarget;
                this.buildTargetGroup = buildTargetGroup;
            }

            public IXRLoaderPreInit loader;
            public BuildTarget buildTarget;
            public BuildTargetGroup buildTargetGroup;
        }

        static private PreInitInfo preInitInfo = null;

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

            XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
            EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);
            if (buildTargetSettings == null)
                return;

            XRGeneralSettings settings = buildTargetSettings.SettingsForBuildTarget(report.summary.platformGroup);
            if (settings == null)
                return;

            // store off some info about the first loader in the list for PreInit boot.config purposes
            preInitInfo = null;
            GameObject loaderManager = settings.LoaderManagerInstance;
            if (loaderManager != null)
            {
                XRManager manager = loaderManager.GetComponent<XRManager>() as XRManager;
                if (manager != null)
                {
                    List<XRLoader> loaders = manager.loaders;
                    if (loaders.Count >= 1)
                    {
                        preInitInfo = new PreInitInfo(loaders[0] as IXRLoaderPreInit, report.summary.platform, report.summary.platformGroup);
                    }
                }
            }

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

            if (preInitInfo == null)
                return;

            // Android build post-processing is handled in OnPostGenerateGradleAndroidProject
            if (report.summary.platform != BuildTarget.Android)
            {
                foreach (BuildFile file in report.files)
                {
                    if (file.role == CommonRoles.bootConfig)
                    {
                        try
                        {
                            string preInitLibraryName = preInitInfo.loader.GetPreInitLibraryName(preInitInfo.buildTarget, preInitInfo.buildTargetGroup);
                            preInitInfo = null;
                            UnityEditor.Experimental.XR.BootOptions.SetXRSDKPreInitLibrary(file.path, preInitLibraryName);
                        }
                        catch (Exception e)
                        {
                            throw new UnityEditor.Build.BuildFailedException(e);
                        }
                        break;
                    }
                }
            }
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (preInitInfo == null)
                return;

            // android builds move the files to a different location than is in the BuildReport, so we have to manually find the boot.config
            string[] paths = { "src", "main", "assets", "bin", "Data", "boot.config" };
            string fullPath = System.IO.Path.Combine(path, String.Join(Path.DirectorySeparatorChar.ToString(), paths));

            try
            {
                string preInitLibraryName = preInitInfo.loader.GetPreInitLibraryName(preInitInfo.buildTarget, preInitInfo.buildTargetGroup);
                preInitInfo = null;
                UnityEditor.Experimental.XR.BootOptions.SetXRSDKPreInitLibrary(fullPath, preInitLibraryName);
            }
            catch (Exception e)
            {
                throw new UnityEditor.Build.BuildFailedException(e);
            }
        }
    }
}

