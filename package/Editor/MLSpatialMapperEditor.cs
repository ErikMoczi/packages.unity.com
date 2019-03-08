using UnityEngine;
using UnityEngine.XR.MagicLeap;

namespace UnityEditor.XR.MagicLeap
{
    [CustomEditor(typeof(MLSpatialMapper))]
    [CanEditMultipleObjects]
    internal class MLSpatialMapperEditor : Editor
    {
        SerializedProperty m_MeshPrefab;
        SerializedProperty m_ComputeNormals;
        SerializedProperty m_LevelOfDetail;
        SerializedProperty m_MeshParent;
        SerializedProperty m_MeshType;
        SerializedProperty m_FillHoleLength;
        SerializedProperty m_MeshQueueSize;
        SerializedProperty m_PollingRate;
        SerializedProperty m_BatchSize;
        SerializedProperty m_Planarize;
        SerializedProperty m_DisconnectedComponentArea;
        SerializedProperty m_RequestVertexConfidence;
        SerializedProperty m_RemoveMeshSkirt;

        bool m_AdvancedOptionsExpanded;

        class Tooltips
        {
            public static readonly GUIContent MeshPrefab = new GUIContent(
                "Mesh Prefab",
                "The prefab which should be instantiated to create individual mesh instances. May have a mesh renderer and an optional mesh collider for physics.");

            public static readonly GUIContent ComputeNormals = new GUIContent(
                "Compute Normals",
                "When enabled, the system will compute the normals for the triangle vertices.");

            public static readonly GUIContent LevelOfDetail = new GUIContent(
                "Level of Detail",
                "The level of detail (LOD) with which to generate meshes. Higher LODs will be more accurate, but take more time to render.");

            public static readonly GUIContent MeshParent = new GUIContent(
                "Mesh Parent",
                "The parent transform for generated meshes.");

            public static readonly GUIContent MeshType = new GUIContent(
                "Type",
                "Whether to generate a triangle mesh or point cloud points.");

            public static readonly GUIContent FillHoleLength = new GUIContent(
                "Fill Hole Length",
                "Boundary distance (in meters) of holes you wish to have filled.");

            public static readonly GUIContent MeshQueueSize = new GUIContent(
                "Mesh Queue Size",
                "Controls the number of meshes to queue for generation at once. Larger numbers will lead to higher CPU usage.");

            public static readonly GUIContent PollingRate = new GUIContent(
                "Update Polling Rate",
                "How often to check for updates, in seconds. More frequent updates will increase CPU usage.");

            public static readonly GUIContent BatchSize = new GUIContent(
                "Batch Size",
                "Maximum number of meshes to update per batch. Larger values are more efficient, but have higher latency.");

            public static readonly GUIContent Planarize = new GUIContent(
                "Planarize",
                "When enabled, the system will planarize the returned mesh (planar regions will be smoothed out).");

            public static readonly GUIContent DisconnectedComponentArea = new GUIContent(
                "Disconnected Component Area",
                "Any component that is disconnected from the main mesh and which has an area less than this size will be removed.");

            public static readonly GUIContent RequestVertexConfidence = new GUIContent(
                "Request Vertex Confidence",
                "When enabled, the system will generate confidence values for each vertex, ranging from 0-1.");

            public static readonly GUIContent RemoveMeshSkirt = new GUIContent(
                "Remove Mesh Skirt",
                "When enabled, the mesh skirt (overlapping area between two mesh blocks) will be removed.");

            public static readonly GUIContent AdvancedOptions = new GUIContent("Advanced");
        }

        protected void OnEnable()
        {
            CacheSerializedProperties();
        }

        public override void OnInspectorGUI()
        {
            this.serializedObject.Update();

            LayoutGUI();

            this.serializedObject.ApplyModifiedProperties();
        }

        void CacheSerializedProperties()
        {
            m_MeshPrefab = this.serializedObject.FindProperty("m_MeshPrefab");
            m_ComputeNormals = this.serializedObject.FindProperty("m_ComputeNormals");
            m_LevelOfDetail = this.serializedObject.FindProperty("m_LevelOfDetail");
            m_MeshParent = this.serializedObject.FindProperty("m_MeshParent");
            m_MeshType = this.serializedObject.FindProperty("m_MeshType");
            m_FillHoleLength = this.serializedObject.FindProperty("m_FillHoleLength");
            m_MeshQueueSize = this.serializedObject.FindProperty("m_MeshQueueSize");
            m_PollingRate = this.serializedObject.FindProperty("m_PollingRate");
            m_BatchSize = this.serializedObject.FindProperty("m_BatchSize");
            m_Planarize = this.serializedObject.FindProperty("m_Planarize");
            m_DisconnectedComponentArea = this.serializedObject.FindProperty("m_DisconnectedComponentArea");
            m_RequestVertexConfidence = this.serializedObject.FindProperty("m_RequestVertexConfidence");
            m_RemoveMeshSkirt = this.serializedObject.FindProperty("m_RemoveMeshSkirt");
        }

        void LayoutGUI()
        {
            EditorGUILayout.PropertyField(m_MeshPrefab, Tooltips.MeshPrefab);
            EditorGUILayout.PropertyField(m_ComputeNormals, Tooltips.ComputeNormals);
            EditorGUILayout.PropertyField(m_MeshParent, Tooltips.MeshParent);
            EditorGUILayout.PropertyField(m_MeshType, Tooltips.MeshType);

            // Advanced options
            var rect = EditorGUILayout.GetControlRect();
            m_AdvancedOptionsExpanded = EditorGUI.Foldout(rect, m_AdvancedOptionsExpanded, Tooltips.AdvancedOptions, true);

            if (m_AdvancedOptionsExpanded)
            {
                EditorGUILayout.PropertyField(m_LevelOfDetail, Tooltips.LevelOfDetail);
                EditorGUILayout.PropertyField(m_MeshQueueSize, Tooltips.MeshQueueSize);
                EditorGUILayout.PropertyField(m_PollingRate, Tooltips.PollingRate);
                EditorGUILayout.PropertyField(m_BatchSize, Tooltips.BatchSize);
                EditorGUILayout.PropertyField(m_FillHoleLength, Tooltips.FillHoleLength);
                m_FillHoleLength.floatValue = Mathf.Max(0f, m_FillHoleLength.floatValue);
                EditorGUILayout.PropertyField(m_Planarize, Tooltips.Planarize);
                EditorGUILayout.PropertyField(m_DisconnectedComponentArea, Tooltips.DisconnectedComponentArea);
                m_DisconnectedComponentArea.floatValue = Mathf.Max(0f, m_DisconnectedComponentArea.floatValue);
                EditorGUILayout.PropertyField(m_RequestVertexConfidence, Tooltips.RequestVertexConfidence);
                EditorGUILayout.PropertyField(m_RemoveMeshSkirt, Tooltips.RemoveMeshSkirt);
            }
        }
    }
}
