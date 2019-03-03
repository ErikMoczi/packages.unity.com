using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.XR.Oculus;

namespace Unity.XR.Oculus.Editor
{
    [CustomEditor(typeof(OculusSettings))]
    public class OculusSettingsEditor : UnityEditor.Editor
    {
        private const string kSharedDepthBuffer = "SharedDepthBuffer";
        private const string kDashSupport = "DashSupport";
        private const string kStereoRenderingMode = "StereoRenderingMode";
        private const string kStereoRenderingModeAndroid = "StereoRenderingModeAndroid";

        static GUIContent s_SharedDepthBufferLabel = EditorGUIUtility.TrTextContent("Shared Depth Buffer");
        static GUIContent s_DashSupportLabel = EditorGUIUtility.TrTextContent("Dash Support");
        static GUIContent s_StereoRenderingMode = EditorGUIUtility.TrTextContent("Stereo Rendering Mode");


        private SerializedProperty m_SharedDepthBuffer;
        private SerializedProperty m_DashSupport;
        private SerializedProperty m_StereoRenderingMode;
        private SerializedProperty m_StereoRenderingModeAndroid;

        public GUIContent AndroidTab;
        public GUIContent WindowsTab;
        private int tab = 0;

        public void OnEnable()
        {
            AndroidTab = new GUIContent("Android",  EditorGUIUtility.IconContent("BuildSettings.Android.Small").image);
            WindowsTab = new GUIContent("Windows",  EditorGUIUtility.IconContent("BuildSettings.Standalone.Small").image);
        }

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;


            if (m_SharedDepthBuffer == null) m_SharedDepthBuffer = serializedObject.FindProperty(kSharedDepthBuffer);
            if (m_DashSupport == null) m_DashSupport = serializedObject.FindProperty(kDashSupport);
            if (m_StereoRenderingMode == null) m_StereoRenderingMode = serializedObject.FindProperty(kStereoRenderingMode);
            if (m_StereoRenderingModeAndroid == null) m_StereoRenderingModeAndroid = serializedObject.FindProperty(kStereoRenderingModeAndroid);

            serializedObject.Update();

            tab = GUILayout.Toolbar(tab, new GUIContent[] {WindowsTab, AndroidTab},EditorStyles.toolbarButton);
            EditorGUILayout.Space();

            EditorGUILayout.BeginVertical(GUILayout.ExpandWidth(true));
            if (tab == 0)
            {
                EditorGUILayout.PropertyField(m_SharedDepthBuffer, s_SharedDepthBufferLabel);
                EditorGUILayout.PropertyField(m_DashSupport, s_DashSupportLabel);
                EditorGUILayout.PropertyField(m_StereoRenderingMode, s_StereoRenderingMode);
            }
            else
            {
                EditorGUILayout.PropertyField(m_StereoRenderingModeAndroid, s_StereoRenderingMode);
            }
            EditorGUILayout.EndVertical();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
