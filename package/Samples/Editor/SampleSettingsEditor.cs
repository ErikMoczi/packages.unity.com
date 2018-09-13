using UnityEditor;
using UnityEditor.AnimatedValues;
using UnityEditor.XR.Management;

using UnityEngine;

namespace UnityEditor.XR.Management.Sample
{

    [CustomEditor(typeof(SampleSettings))]
    public class SampleSettingsEditor : Editor
    {
        static string kRequiresProperty = "m_RequiresItem";
        static string kRuntimeToggleProperty  = "m_RuntimeToggle";

        static GUIContent kShowBuildSettingsLabel = new GUIContent("Build Settings");
        static GUIContent kRequiresLabel = new GUIContent("Item Requirement");

        static GUIContent kShowRuntimeSettingsLabel = new GUIContent("Runtime Settings");
        static GUIContent kRuntimeToggleLabel = new GUIContent("Should I stay or should I go?");

        bool showBuildSettings = true;
        bool showRuntimeSettings = true;

        SerializedProperty m_RequiesItemProperty;
        SerializedProperty m_RuntimeToggleProperty;

        public override void OnInspectorGUI()
        {
            if (serializedObject == null || serializedObject.targetObject == null)
                return;

            if (m_RequiesItemProperty == null) m_RequiesItemProperty = serializedObject.FindProperty(kRequiresProperty);
            if (m_RuntimeToggleProperty == null) m_RuntimeToggleProperty = serializedObject.FindProperty(kRuntimeToggleProperty);

            serializedObject.Update();
            showBuildSettings = EditorGUILayout.Foldout(showBuildSettings, kShowBuildSettingsLabel);
            if (showBuildSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RequiesItemProperty, kRequiresLabel);
                EditorGUI.indentLevel--;
            }

            EditorGUILayout.Space();

            showRuntimeSettings = EditorGUILayout.Foldout(showRuntimeSettings, kShowRuntimeSettingsLabel);
            if (showRuntimeSettings)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_RuntimeToggleProperty, kRuntimeToggleLabel);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
