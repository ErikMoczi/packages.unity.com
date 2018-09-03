#if UNITY_IOS
using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System;
using UnityEditor.iOS.Xcode;

internal class UnityARKitPostBuild
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
}
#endif
