using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(RigTransform))]
    public class RigTransformEditor : Editor
    {
        SerializedProperty m_Sync;

        void OnEnable()
        {
            m_Sync = serializedObject.FindProperty("syncFromScene");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.Separator();
            EditorGUILayout.PropertyField(m_Sync);
            serializedObject.ApplyModifiedProperties();
        }
    }
}