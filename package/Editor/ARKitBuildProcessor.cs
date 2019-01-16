#if UNITY_IOS
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Xml;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Compilation;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.XR.ARExtensions;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.XR.ARKit
{
    internal class ARKitBuildProcessor
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
                return;

            HandleARKitRequiredFlag(pathToBuiltProject);
        }

        static void HandleARKitRequiredFlag(string pathToBuiltProject)
        {
            var arkitSettings = ARKitSettings.GetOrCreateSettings();
            string plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));
            PlistElementDict rootDict = plist.root;

            // Get or create array to manage device capabilities
            const string capsKey = "UIRequiredDeviceCapabilities";
            PlistElementArray capsArray;
            PlistElement pel;
            if (rootDict.values.TryGetValue(capsKey, out pel))
            {
                capsArray = pel.AsArray();
            }
            else
            {
                capsArray = rootDict.CreateArray(capsKey);
            }
            // Remove any existing "arkit" plist entries
            const string arkitStr = "arkit";
            capsArray.values.RemoveAll(x => arkitStr.Equals(x.AsString()));
            if (arkitSettings.ARKitRequirement == ARKitSettings.Requirement.Required)
            {
                // Add "arkit" plist entry
                capsArray.AddString(arkitStr);
            }

            File.WriteAllText(plistPath, plist.WriteToString());
        }

        internal class ARKitPreprocessBuild : IPreprocessBuildWithReport
        {
#pragma warning disable 649
            // type used to load the .asmdef as json
            struct AssemblyDefinitionType
            {
                public string name;
                public List<string> references;
                public List<string> includePlatforms;
                public List<string> excludePlatforms;
                public bool allowUnsafeCode;
                public bool autoReferenced;
            }
#pragma warning restore 649

            // Magic value according to
            // https://docs.unity3d.com/ScriptReference/PlayerSettings.GetArchitecture.html
            // "0 - None, 1 - ARM64, 2 - Universal."
            const int k_TargetArchitectureArm64 = 1;

            public void OnPreprocessBuild(BuildReport report)
            {
                if (report.summary.platform != BuildTarget.iOS)
                    return;

                if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                    throw new BuildFailedException("ARKit requires a Camera Usage Description (Player Settings > iOS > Other Settings > Camera Usage Description)");

                EnsureOnlyMetalIsUsed();
                EnsureTargetArchitecturesAreSupported(report.summary.platformGroup);

                var arkitSettings = ARKitSettings.GetOrCreateSettings();

                if (LinkerUtility.AssemblyStrippingEnabled(report.summary.platformGroup))
                {
                    LinkerUtility.EnsureLinkXmlExistsFor("ARKit");
                    if (arkitSettings.ARKitFaceTrackingEnabled)
                        LinkerUtility.EnsureLinkXmlExistsFor("ARKit.FaceTracking");
                }

                SetFaceTrackingAssemblyIncludePlatformIos(arkitSettings.ARKitFaceTrackingEnabled);

                var pluginImporter = AssetImporter.GetAtPath("Packages/com.unity.xr.arkit/Runtime/iOS/libUnityARKitFaceTracking.a") as PluginImporter;
                if (pluginImporter)
                    pluginImporter.SetCompatibleWithPlatform(BuildTarget.iOS, arkitSettings.ARKitFaceTrackingEnabled);
            }

            void SetFaceTrackingAssemblyIncludePlatformIos(bool includeIos)
            {
                bool needToUpdate = false;

                var faceTrackingAssemblyPath =
                    CompilationPipeline.GetAssemblyDefinitionFilePathFromAssemblyName("Unity.XR.ARKit.FaceTracking");

                if (string.IsNullOrEmpty(faceTrackingAssemblyPath))
                {
                    if (includeIos)
                        throw new BuildFailedException("Assembly definition file for Unity.XR.ARKit.FaceTracking not found. This is required for face tracking.");
                    return;
                }

                var faceTrackingAssembly = JsonUtility.FromJson<AssemblyDefinitionType>(File.ReadAllText(faceTrackingAssemblyPath));

                if (includeIos)
                {
                    // Enable face tracking
                    if (!faceTrackingAssembly.includePlatforms.Contains("iOS"))
                    {
                        faceTrackingAssembly.includePlatforms.Add("iOS");
                        needToUpdate = true;
                    }
                }
                else
                {
                    // Disable face tracking
                    if (faceTrackingAssembly.includePlatforms.Contains("iOS"))
                    {
                        faceTrackingAssembly.includePlatforms.Remove("iOS");
                        needToUpdate = true;
                    }
                }

                if (needToUpdate)
                {
                    File.WriteAllText(faceTrackingAssemblyPath, JsonUtility.ToJson(faceTrackingAssembly, true));
                    AssetDatabase.ImportAsset(faceTrackingAssemblyPath);
                }
            }

            void EnsureTargetArchitecturesAreSupported(BuildTargetGroup buildTargetGroup)
            {
                if (PlayerSettings.GetArchitecture(buildTargetGroup) != k_TargetArchitectureArm64)
                    throw new BuildFailedException("ARKit XR Plugin only supports the ARM64 architecture. See Player Settings > Other Settings > Architecture.");
            }

            void EnsureOnlyMetalIsUsed()
            {
                var graphicsApis = PlayerSettings.GetGraphicsAPIs(BuildTarget.iOS);
                if (graphicsApis.Length > 0)
                {
                    var graphicsApi = graphicsApis[0];
                    if (graphicsApi != GraphicsDeviceType.Metal)
                        throw new BuildFailedException("You have selected the graphics API " + graphicsApi + ". Only the Metal graphics API is supported by the ARKit XR Plugin. (See Player Settings > Other Settings > Graphics APIs)");
                }
            }

            public int callbackOrder { get { return 0; } }
        }
    }
}
#endif
