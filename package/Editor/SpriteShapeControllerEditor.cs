using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Experimental.U2D.Common;
using UnityEditor.AnimatedValues;

namespace UnityEditor.U2D
{
    [System.Serializable]
    [CustomEditor(typeof(SpriteShapeController))]
    [CanEditMultipleObjects]
    internal class SpriteShapeControllerEditor : Editor
    {
        private static class Contents
        {
            public static readonly GUIContent splineLabel = new GUIContent("Spline");
            public static readonly string editSplineLabel = "Edit Spline";
            public static readonly GUIContent fillLabel = new GUIContent("Fill");
            public static readonly GUIContent colliderLabel = new GUIContent("Collider");
            public static readonly GUIContent fillPixelPerUnitLabel = new GUIContent("Pixel Per Unit", "Pixel Per Unit for Fill Texture.");
            public static readonly GUIContent spriteShapeProfile = new GUIContent("Profile", "The SpriteShape Profile to render");
            public static readonly GUIContent materialLabel = new GUIContent("Material", "Material to be used by SpriteRenderer");
            public static readonly GUIContent colorLabel = new GUIContent("Color", "Rendering color for the Sprite graphic");
            public static readonly GUIContent metaDataLabel = new GUIContent("Meta Data", "SpriteShape Specific Control Point Data");
            public static readonly GUIContent showComponentsLabel = new GUIContent("Show Render Stuff", "Show Renderer Components.");
            public static readonly GUIContent[] splineDetailOptions = { new GUIContent("High Quality"), new GUIContent("Medium Quality"), new GUIContent("Low Quality") };
            public static readonly GUIContent splineDetail = new GUIContent("Detail", "Tessellation Quality for rendering.");
            public static readonly GUIContent openEndedLabel = new GUIContent("Open Ended", "Is the path open ended or closed.");
            public static readonly GUIContent adaptiveUVLabel = new GUIContent("Adaptive UV", "Allow Adaptive UV Generation");
            public static readonly GUIContent worldUVLabel = new GUIContent("Worldspace UV", "Generate UV for world space.");
            public static readonly GUIContent stretchUVLabel = new GUIContent("Stretch UV", "Stretch the Fill UV to full Rect.");
            public static readonly GUIContent stretchTilingLabel = new GUIContent("Stretch Tiling", "Stretch Tiling Count.");
            public static readonly GUIContent colliderDetail = new GUIContent("Detail", "Tessellation Quality on the collider.");
            public static readonly GUIContent colliderOffset = new GUIContent("Offset", "Extrude collider distance.");
            public static readonly GUIContent updateColliderLabel = new GUIContent("Update Collider", "Update Collider as you Edit SpriteShape");
            public static readonly GUIContent optimizeColliderLabel = new GUIContent("Optimize Collider", "Cleanup planar self-Intersections and Optimize Collider Points");
        }
        
        SpriteShapeToolEditor m_SplineEditor;

        private SerializedProperty m_SpriteShapeProp;
        private SerializedProperty m_SplineDetailProp;
        private SerializedProperty m_IsOpenEndedProp;
        private SerializedProperty m_AdaptiveUVProp;
        private SerializedProperty m_StretchUVProp;
        private SerializedProperty m_StretchTilingProp;
        private SerializedProperty m_WorldSpaceUVProp;
        private SerializedProperty m_FillPixelPerUnitProp;
        private SerializedProperty m_SortingLayerProp;
        private SerializedProperty m_SortingOrderProp;

        private SerializedProperty m_ColliderAutoUpdate;
        private SerializedProperty m_ColliderDetailProp;
        private SerializedProperty m_ColliderOffsetProp;

        private SerializedProperty m_OptimizeColliderProp;
        private SerializedObject m_MeshRendererSO;
        private int m_CollidersCount = 0;

        private int[] m_QualityValues = new int[] { (int)QualityDetail.High, (int)QualityDetail.Mid, (int)QualityDetail.Low };
        readonly AnimBool m_ShowStretchOption = new AnimBool();
        readonly AnimBool m_ShowNonStretchOption = new AnimBool();

        public UnityEngine.Object undoObject
        {
            get { return target; }
        }

        private SpriteShapeController m_SpriteShapeController { get { return target as SpriteShapeController; } }

        private void OnEnable()
        {
            m_SpriteShapeProp = serializedObject.FindProperty("m_SpriteShape");
            m_SplineDetailProp = serializedObject.FindProperty("m_SplineDetail");
            m_IsOpenEndedProp = serializedObject.FindProperty("m_Spline").FindPropertyRelative("m_IsOpenEnded");
            m_AdaptiveUVProp = serializedObject.FindProperty("m_AdaptiveUV");
            m_StretchUVProp = serializedObject.FindProperty("m_StretchUV");
            m_StretchTilingProp = serializedObject.FindProperty("m_StretchTiling");
            m_WorldSpaceUVProp = serializedObject.FindProperty("m_WorldSpaceUV");
            m_FillPixelPerUnitProp = serializedObject.FindProperty("m_FillPixelPerUnit");

            m_ColliderAutoUpdate = serializedObject.FindProperty("m_UpdateCollider");
            m_ColliderDetailProp = serializedObject.FindProperty("m_ColliderDetail");
            m_ColliderOffsetProp = serializedObject.FindProperty("m_ColliderOffset");
            m_OptimizeColliderProp = serializedObject.FindProperty("m_OptimizeCollider");

            if (m_SpriteShapeController.spriteShapeRenderer)
            {
                m_MeshRendererSO = new SerializedObject(m_SpriteShapeController.spriteShapeRenderer);
                m_SortingLayerProp = m_MeshRendererSO.FindProperty("m_SortingLayerID");
                m_SortingOrderProp = m_MeshRendererSO.FindProperty("m_SortingOrder");
            }

            m_SplineEditor = new SpriteShapeToolEditor();

            m_ShowStretchOption.valueChanged.AddListener(Repaint);
            m_ShowStretchOption.value = ShouldShowStretchOption();

            m_ShowNonStretchOption.valueChanged.AddListener(Repaint);
            m_ShowNonStretchOption.value = !ShouldShowStretchOption();
        }

        private void OnDestroy()
        {
            if (m_SplineEditor != null)
                m_SplineEditor.OnDisable();
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

        public void DrawHeader(GUIContent content)
        {
            EditorGUILayout.LabelField(content, EditorStyles.boldLabel);
        }

        private bool ShouldShowStretchOption()
        {
            return m_StretchUVProp.boolValue;
        }

        public override void OnInspectorGUI()
        {
            var updateCollider = false;
            EditorGUI.BeginChangeCheck();

            serializedObject.Update();

            EditorGUILayout.PropertyField(m_SpriteShapeProp, Contents.spriteShapeProfile);

            DoEditSplineButton();

            using (new EditorGUI.DisabledGroupScope(SpriteShapeTool.instance.isActive == false))
            {
                m_SplineEditor.OnInspectorGUI(m_SpriteShapeController.spline);
            }

            EditorGUILayout.Space();
            DrawHeader(Contents.splineLabel);
            EditorGUILayout.IntPopup(m_SplineDetailProp, Contents.splineDetailOptions, m_QualityValues, Contents.splineDetail);
            EditorGUILayout.PropertyField(m_IsOpenEndedProp, Contents.openEndedLabel);
            EditorGUILayout.PropertyField(m_AdaptiveUVProp, Contents.adaptiveUVLabel);

            EditorGUILayout.Space();
            DrawHeader(Contents.fillLabel);
            EditorGUILayout.PropertyField(m_StretchUVProp, Contents.stretchUVLabel);

            if (ShouldShowStretchOption())
            {
                EditorGUILayout.PropertyField(m_StretchTilingProp, Contents.stretchTilingLabel);
            }
            else
            {
                EditorGUILayout.PropertyField(m_FillPixelPerUnitProp, Contents.fillPixelPerUnitLabel);
                EditorGUILayout.PropertyField(m_WorldSpaceUVProp, Contents.worldUVLabel);
            }

            if (m_SpriteShapeController.gameObject.GetComponent<PolygonCollider2D>() != null || m_SpriteShapeController.gameObject.GetComponent<EdgeCollider2D>() != null)
            {
                EditorGUILayout.Space();
                DrawHeader(Contents.colliderLabel);
                EditorGUILayout.PropertyField(m_ColliderAutoUpdate, Contents.updateColliderLabel);
                EditorGUILayout.IntPopup(m_ColliderDetailProp, Contents.splineDetailOptions, m_QualityValues, Contents.colliderDetail);
                EditorGUILayout.PropertyField(m_ColliderOffsetProp, Contents.colliderOffset);
                EditorGUILayout.PropertyField(m_OptimizeColliderProp, Contents.optimizeColliderLabel);
            }
            if (EditorGUI.EndChangeCheck())
                updateCollider = true;

            serializedObject.ApplyModifiedProperties();

            if (updateCollider || OnCollidersAddedOrRemoved())
                BakeCollider();
        }

        private void DoEditSplineButton()
        {
            GUIStyle singleButtonStyle = "EditModeSingleButton";
            const float k_EditColliderbuttonWidth = 33;
            const float k_EditColliderbuttonHeight = 23;
            const float k_SpaceBetweenLabelAndButton = 5;
            var label = Contents.editSplineLabel;
            var icon = SpriteShapeEditorTool.Contents.icon;

            var rect = EditorGUILayout.GetControlRect(true, k_EditColliderbuttonHeight, singleButtonStyle);
            var buttonRect = new Rect(rect.xMin + EditorGUIUtility.labelWidth, rect.yMin, k_EditColliderbuttonWidth, k_EditColliderbuttonHeight);

            var labelContent = new GUIContent(label);
            var labelSize = GUI.skin.label.CalcSize(labelContent);

            var labelRect = new Rect(
                buttonRect.xMax + k_SpaceBetweenLabelAndButton,
                rect.yMin + (rect.height - labelSize.y) * .5f,
                labelSize.x,
                rect.height);

            var disable = SpriteShapeEditorTool.instance != null && !SpriteShapeEditorTool.instance.IsAvailable();
            var isActive = SpriteShapeEditorTool.instance != null && SpriteShapeEditorTool.instance.isActive;

            using (new EditorGUI.DisabledGroupScope(disable))
            {
                EditorGUI.BeginChangeCheck();

                isActive = GUI.Toggle(buttonRect, isActive, icon, singleButtonStyle);
                GUI.Label(labelRect, label);

                if (EditorGUI.EndChangeCheck())
                {
                    if (isActive)
                        EditorTools.EditorTools.SetActiveTool<SpriteShapeEditorTool>();
                    else
                        EditorTools.EditorTools.RestorePreviousTool();
                }
            }
        }

        void BakeCollider()
        {
            if (m_SpriteShapeController.autoUpdateCollider == false)
                return;

            PolygonCollider2D polygonCollider = m_SpriteShapeController.polygonCollider;
            if (polygonCollider)
            {
                Undo.RegisterCompleteObjectUndo(polygonCollider, Undo.GetCurrentGroupName());
                EditorUtility.SetDirty(polygonCollider);
                m_SpriteShapeController.RefreshSpriteShape();
            }

            EdgeCollider2D edgeCollider = m_SpriteShapeController.edgeCollider;
            if (edgeCollider)
            {
                Undo.RegisterCompleteObjectUndo(edgeCollider, Undo.GetCurrentGroupName());
                EditorUtility.SetDirty(edgeCollider);
                m_SpriteShapeController.RefreshSpriteShape();

            }
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
