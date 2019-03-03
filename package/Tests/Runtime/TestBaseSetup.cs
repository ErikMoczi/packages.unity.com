using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.SpatialTracking;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal.VR;
#endif

[PrebuildSetup(typeof(TestPrebuildSetup))]
public class TestBaseSetup : MonoBehaviour
{
    public static GameObject m_Camera;
    public static GameObject m_Light;
    public static GameObject m_Cube;

    public static GameObject m_XrManager;
    public static GameObject m_TrackingRig;
    public static TrackedPoseDriver m_TrackHead;

    public TestSetupHelpers m_TestSetupHelpers;

    [SetUp]
    public void XrSdkTestBaseSetup()
    {
        m_Cube = new GameObject("Cube");
        m_TestSetupHelpers = new TestSetupHelpers();

        m_TestSetupHelpers.TestStageSetup(TestStageConfig.BaseStageSetup);
    }

    [TearDown]
    public void XrSdkTestBaseTearDown()
    {
        m_TestSetupHelpers.TestStageSetup(TestStageConfig.CleanStage);
    }

    public class TestPrebuildSetup : IPrebuildSetup
    {
        public void Setup()
        {
            // Configure StandAlone build
            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneWindows64);
            EditorUserBuildSettings.wsaUWPBuildType = WSAUWPBuildType.D3D;
            EditorUserBuildSettings.allowDebugging = true;

            PlayerSettings.SetScriptingBackend(BuildTargetGroup.Standalone, ScriptingImplementation.IL2CPP);
        }
    }
}
