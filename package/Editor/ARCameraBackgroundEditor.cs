using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace UnityEditor.XR.ARFoundation
{
    [CustomEditor(typeof(ARCameraBackground))]
    internal class ARCameraBackgroundEditor : Editor
    {
        SerializedProperty m_OverrideMaterial;

        SerializedProperty m_Material;

        static class Tooltips
        {
            public static readonly GUIContent overrideMaterial = new GUIContent(
                "Override Material",
                "When false, a material is generated automatically from the shader included in the platform-specific package. You may override this material if you wish.");

            public static readonly GUIContent material = new GUIContent(
                "Material",
                "The material to use for background rendering.");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_OverrideMaterial, Tooltips.overrideMaterial);

            if (m_OverrideMaterial.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Material, Tooltips.material);
                EditorGUI.indentLevel--;
            }

            serializedObject.ApplyModifiedProperties();
        }

        void OnEnable()
        {
            m_OverrideMaterial = serializedObject.FindProperty("m_OverrideMaterial");
            m_Material = serializedObject.FindProperty("m_Material");
        }
    }
}
