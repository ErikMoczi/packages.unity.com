using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Experimental.U2D.Common;

namespace UnityEditor.U2D
{
    [System.Serializable]
    [CustomEditor(typeof(SpriteShapeController))]
    [CanEditMultipleObjects]
    internal class SpriteShapeControllerEditor : Editor
    {
        SplineEditor m_SplineEditor;
        SplineSceneEditor m_SplineSceneEditor;

        private SerializedProperty m_SpriteShapeProp;
        private SerializedProperty m_SplineDetailProp;
        private SerializedProperty m_IsOpenEndedProp;
        private SerializedProperty m_AdaptiveUVProp;
        private SerializedProperty m_SortingLayerProp;
        private SerializedProperty m_SortingOrderProp;

        private SerializedProperty m_ColliderAutoUpdate;
        private SerializedProperty m_ColliderDetailProp;
        private SerializedProperty m_ColliderOffsetProp;
        private SerializedProperty m_ColliderCornerTypeProp;

        private SerializedObject m_MeshRendererSO;
        private int m_CollidersCount = 0;

        private int[] m_QualityValues = new int[] { (int)QualityDetail.High, (int)QualityDetail.Mid, (int)QualityDetail.Low };

        public UnityEngine.Object undoObject
        {
            get { return target;  }
        }

        private static class Contents
        {
            public static readonly GUIContent spriteShape = new GUIContent("SpriteShape", "The SpriteShape to render");
            public static readonly GUIContent materialLabel = new GUIContent("Material", "Material to be used by SpriteRenderer");
            public static readonly GUIContent colorLabel = new GUIContent("Color", "Rendering color for the Sprite graphic");
            public static readonly GUIContent metaDataLabel = new GUIContent("Meta Data", "SpriteShape Specific Control Point Data");
            public static readonly GUIContent showComponentsLabel = new GUIContent("Show Render Stuff", "Show Renderer Components.");
            public static readonly GUIContent[] splineDetailOptions = { new GUIContent("High Quality"), new GUIContent("Medium Quality"), new GUIContent("Low Quality") };
            public static readonly GUIContent splineDetail = new GUIContent("Detail", "Tessellation Quality for rendering.");
            public static readonly GUIContent openEndedLabel = new GUIContent("Open Ended", "Is the path open ended or closed.");
            public static readonly GUIContent adaptiveUVLabel = new GUIContent("Adaptive UV", "Allow Adaptive UV Generation");
            public static readonly GUIContent colliderDetail = new GUIContent("Detail", "Tessellation Quality on the collider.");
            public static readonly GUIContent colliderOffset = new GUIContent("Offset", "Extrude collider distance.");
            public static readonly GUIContent colliderCornerType = new GUIContent("Corner Type", "How the collider should adapt in corners");
            public static readonly GUIContent splineLabel = new GUIContent("Spline");
            public static readonly GUIContent colliderLabel = new GUIContent("Collider");
            public static readonly GUIContent updateColliderLabel = new GUIContent("Update Collider");
        }

        private SpriteShapeController m_SpriteShapeController { get { return target as SpriteShapeController; } }

        private void OnEnable()
        {
            m_SpriteShapeProp = serializedObject.FindProperty("m_SpriteShape");
            m_SplineDetailProp = serializedObject.FindProperty("m_SplineDetail");
            m_IsOpenEndedProp = serializedObject.FindProperty("m_Spline").FindPropertyRelative("m_IsOpenEnded");
            m_AdaptiveUVProp = serializedObject.FindProperty("m_AdaptiveUV");

            m_ColliderAutoUpdate = serializedObject.FindProperty("m_UpdateCollider");
            m_ColliderDetailProp = serializedObject.FindProperty("m_ColliderDetail");
            m_ColliderOffsetProp = serializedObject.FindProperty("m_ColliderOffset");
            m_ColliderCornerTypeProp = serializedObject.FindProperty("m_ColliderCornerType");

            if (m_SpriteShapeController.spriteShapeRenderer)
            {
                m_MeshRendererSO = new SerializedObject(m_SpriteShapeController.spriteShapeRenderer);
                m_SortingLayerProp = m_MeshRendererSO.FindProperty("m_SortingLayerID");
                m_SortingOrderProp = m_MeshRendererSO.FindProperty("m_SortingOrder");
            }

            m_SplineEditor = new SplineEditor(this, m_SpriteShapeController.spriteShape);
            m_SplineSceneEditor = new SplineSceneEditor(m_SpriteShapeController.spline, this, m_SpriteShapeController);

            EditMode.onEditModeStartDelegate += OnEditModeStart;
            EditMode.onEditModeEndDelegate += OnEditModeEnd;
        }

        private void OnDestroy()
        {
            if (m_SplineEditor != null)
                m_SplineEditor.OnDisable();
            if (m_SplineSceneEditor != null)
                m_SplineSceneEditor.OnDisable();

            OnEditModeEnd(this);

            EditMode.onEditModeEndDelegate -= OnEditModeEnd;
        }

        private void OnEditModeStart(Editor editor, EditMode.SceneViewEditMode mode)
        {
            if (editor == this)
            {
                EditorUtility.SetSelectedRenderState(m_SpriteShapeController.spriteShapeRenderer, EditorSelectedRenderState.Hidden);
            }
        }

        private void OnEditModeEnd(Editor editor)
        {
            if (editor == this && m_SpriteShapeController)
            {
                ShapeEditorCache.InvalidateCache();
                EditorUtility.SetSelectedRenderState(m_SpriteShapeController.spriteShapeRenderer, EditorSelectedRenderState.Wireframe | EditorSelectedRenderState.Highlight);
            }
        }

        private bool OnCollidersAddedOrRemoved()
        {
            PolygonCollider2D polygonCollider = m_SpriteShapeController.polygonCollider;
            EdgeCollider2D edgeCollider = m_SpriteShapeController.edgeCollider;
            int collidersCount = 0;

            if (polygonCollider != null)
                collidersCount = collidersCount + 1;
            if (edgeCollider != null)
                collidersCount = collidersCount + 1;

            if (collidersCount != m_CollidersCount)
            {
                m_CollidersCount = collidersCount;
                return true;
            }
            return false;
        }

        private void ValidateSelection()
        {
            int pointCount = m_SpriteShapeController.spline.GetPointCount();

            bool selectionValid = true;

            ISelection selection = ShapeEditorCache.GetSelection();

            foreach (int index in selection)
            {
                if (index >= pointCount)
                {
                    selectionValid = false;
                    break;
                }
            }

            if (!selectionValid)
            {
                ShapeEditorCache.RecordUndo();
                ShapeEditorCache.ClearSelection();
            }
        }

        public void DrawHeader(GUIContent content)
        {
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        public override void OnInspectorGUI()
        {
            bool updateCollider = false;

            ValidateSelection();

            EditorGUI.BeginChangeCheck();

            m_SplineSceneEditor.OnInspectorGUI();
            m_SplineEditor.OnInspectorGUI(m_SpriteShapeController.spline);

            EditorGUILayout.Space();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SpriteShapeProp, Contents.spriteShape);

            EditorGUILayout.Space();
            DrawHeader(Contents.splineLabel);
            EditorGUILayout.IntPopup(m_SplineDetailProp, Contents.splineDetailOptions, m_QualityValues, Contents.splineDetail);
            EditorGUILayout.PropertyField(m_IsOpenEndedProp, Contents.openEndedLabel);
            EditorGUILayout.PropertyField(m_AdaptiveUVProp, Contents.adaptiveUVLabel);

            if (m_SpriteShapeController.gameObject.GetComponent<PolygonCollider2D>() != null || m_SpriteShapeController.gameObject.GetComponent<EdgeCollider2D>() != null)
            {
                EditorGUILayout.Space();
                DrawHeader(Contents.colliderLabel);
                EditorGUILayout.PropertyField(m_ColliderAutoUpdate, Contents.updateColliderLabel);
                EditorGUILayout.IntPopup(m_ColliderDetailProp, Contents.splineDetailOptions, m_QualityValues, Contents.colliderDetail);
                EditorGUILayout.PropertyField(m_ColliderCornerTypeProp, Contents.colliderCornerType);
                EditorGUILayout.PropertyField(m_ColliderOffsetProp, Contents.colliderOffset);
            }
            if (EditorGUI.EndChangeCheck())
                updateCollider = true;

            serializedObject.ApplyModifiedProperties();

            if (updateCollider || OnCollidersAddedOrRemoved())
                BakeCollider();
        }

        public void OnSceneGUI()
        {
            m_SplineSceneEditor.CalculateBounds();

            if (!EditMode.IsOwner(this))
                return;

            EditorGUI.BeginChangeCheck();

            m_SplineSceneEditor.OnSceneGUI();
            m_SplineEditor.HandleHotKeys();

            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(m_SpriteShapeController);
                BakeCollider();
            }
        }

        void BakeCollider()
        {
            if (m_SpriteShapeController.autoUpdateCollider == false)
                return;

            PolygonCollider2D polygonCollider = m_SpriteShapeController.polygonCollider;
            EdgeCollider2D edgeCollider = m_SpriteShapeController.edgeCollider;

            if (polygonCollider)
            {
                Undo.RegisterCompleteObjectUndo(polygonCollider, Undo.GetCurrentGroupName());
                EditorUtility.SetDirty(polygonCollider);
            }

            if (edgeCollider)
            {
                Undo.RegisterCompleteObjectUndo(edgeCollider, Undo.GetCurrentGroupName());
                EditorUtility.SetDirty(edgeCollider);
            }

            m_SpriteShapeController.BakeCollider();
        }

        void RenderSortingLayerFields()
        {
            if (m_MeshRendererSO != null)
            {
                m_MeshRendererSO.Update();

                InternalEditorBridge.RenderSortingLayerFields(m_SortingOrderProp, m_SortingLayerProp);

                m_MeshRendererSO.ApplyModifiedProperties();
            }
        }

        void ShowMaterials(bool show)
        {
            HideFlags hideFlags = HideFlags.HideInInspector;

            if (show)
                hideFlags = HideFlags.None;

            Material[] materials = m_SpriteShapeController.spriteShapeRenderer.sharedMaterials;

            foreach (Material material in materials)
            {
                material.hideFlags = hideFlags;
                EditorUtility.SetDirty(material);
            }
        }

        [DrawGizmo(GizmoType.InSelectionHierarchy)]
        static void RenderSpline(SpriteShapeController m_SpriteShapeController, GizmoType gizmoType)
        {
            Spline m_Spline = m_SpriteShapeController.spline;
            Matrix4x4 oldMatrix = Handles.matrix;
            Handles.matrix = m_SpriteShapeController.transform.localToWorldMatrix;
            int points = m_Spline.GetPointCount();
            for (int i = 0; i < (m_Spline.isOpenEnded ? points - 1 : points); i++)
            {
                Vector3 p1 = m_Spline.GetPosition(i);
                Vector3 p2 = m_Spline.GetPosition((i + 1) % points);
                var t1 = p1 + m_Spline.GetRightTangent(i);
                var t2 = p2 + m_Spline.GetLeftTangent((i + 1) % points);
                Handles.DrawBezier(p1, p2, t1, t2, Color.gray, null, 2f);
            }
            Handles.matrix = oldMatrix;
        }
    }
}
