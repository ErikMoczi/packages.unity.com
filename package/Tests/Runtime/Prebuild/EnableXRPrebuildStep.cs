using System;
using System.Collections;

using NUnit.Framework;

#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
#endif
using UnityEngine.TestTools;


public class EnableXRPrebuildStep : IPrebuildSetup
{
    private const string kLuminSdkEnvironment = "LUMINSDK_UNITY";
    public void Setup()
    {
#if UNITY_EDITOR && PLATFORM_LUMIN
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Lumin, "com.unity.luminplaymodetest");
        PlayerSettings.virtualRealitySupported = true;
        UnityEditorInternal.VR.VREditor.SetVirtualRealitySDKs(BuildTargetGroup.Lumin, new[] { "lumin" });
        var sdk = Environment.GetEnvironmentVariable(kLuminSdkEnvironment);
        if (!string.IsNullOrEmpty(sdk))
        {
            UnityEditor.Lumin.UserBuildSettings.SDKPath = sdk;
        }
#endif // UNITY_EDITOR && PLATFORM_LUMIN
    }
}
