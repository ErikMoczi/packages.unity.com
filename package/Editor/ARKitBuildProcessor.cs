#if UNITY_IOS
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;

namespace UnityEditor.XR.ARKit
{
    internal class ARKitBuildProcessor
    {
        [PostProcessBuildAttribute(1)]
        public static void OnPostprocessBuild(BuildTarget target, string pathToBuiltProject)
        {
            if (target != BuildTarget.iOS)
                return;

            string unityTargetName = PBXProject.GetUnityTargetName();
            string projPath = PBXProject.GetPBXProjectPath(pathToBuiltProject);

            // Create a PBXProject object and populate it with the trampoline project
            var proj = new PBXProject();
            proj.ReadFromString(File.ReadAllText(projPath));

            // Add the ARKit framework to the Xcode project
            string targetGuid = proj.TargetGuidByName(unityTargetName);
            const bool isFrameworkOptional = false;
            const string arkitFramework = "ARKit.framework";
            proj.AddFrameworkToProject(targetGuid, arkitFramework, isFrameworkOptional);

            // Finally, write out the modified project with the framework added.
            File.WriteAllText(projPath, proj.WriteToString());
        }

        internal class ARKitPreprocessBuild : IPreprocessBuildWithReport
        {
            public void OnPreprocessBuild(BuildReport report)
            {
                if (report.summary.platform != BuildTarget.iOS)
                    return;

                if (string.IsNullOrEmpty(PlayerSettings.iOS.cameraUsageDescription))
                    throw new BuildFailedException("ARKit requires a Camera Usage Description (Player Settings > iOS > Other Settings > Camera Usage Description)");

                EnsureOnlyMetalIsUsed();

#if !UNITY_2018_3_OR_NEWER
                if ((report.summary.options & BuildOptions.SymlinkLibraries) != BuildOptions.None)
                    throw new BuildFailedException("The \"ARKit XR Plugin\" package cannot be symlinked. Go to File > Build Settings... and uncheck \"Symlink Unity libraries\".");
#endif
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
