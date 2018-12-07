using UnityEngine.Animations.Rigging;

namespace UnityEditor.Animations.Rigging
{
    [CustomEditor(typeof(BoneRenderer))]
    [CanEditMultipleObjects]
    public class BoneRendererInspector : Editor
    {
        SerializedProperty m_Shape;
        SerializedProperty m_DrawSkeleton;
        SerializedProperty m_DrawTripods;
        SerializedProperty m_BoneSize;
        SerializedProperty m_SkeletonColor;
        SerializedProperty m_Transforms;

        public void OnEnable()
        {
            m_Shape = serializedObject.FindProperty("shape");
            m_DrawSkeleton = serializedObject.FindProperty("drawSkeleton");
            m_DrawTripods = serializedObject.FindProperty("drawTripods");
            m_BoneSize = serializedObject.FindProperty("boneSize");
            m_SkeletonColor = serializedObject.FindProperty("skeletonColor");
            m_Transforms = serializedObject.FindProperty("m_Transforms");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_Shape);
            EditorGUILayout.PropertyField(m_DrawSkeleton);
            EditorGUILayout.PropertyField(m_DrawTripods);
            EditorGUILayout.PropertyField(m_BoneSize);
            EditorGUILayout.PropertyField(m_SkeletonColor);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_Transforms, true);
            bool boneRendererDirty = EditorGUI.EndChangeCheck();

            serializedObject.ApplyModifiedProperties();

            if (boneRendererDirty)
            {
                for (int i = 0; i < targets.Length; i++)
                {
                    var boneRenderer = targets[i] as BoneRenderer;
                    boneRenderer.ExtractBones();
                }
            }
        }
    }
}
