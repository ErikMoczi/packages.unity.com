using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.U2D
{
    public class SplineSceneEditor
    {
        private static class Contents
        {
            public static readonly GUIContent editShapeIcon = SpriteShapeEditorGUI.IconContent("EditShape", "Start editing the m_Spline in the scene view.");
            public static readonly GUIContent editShapeIconPro = SpriteShapeEditorGUI.IconContent("EditShapePro", "Start editing the m_Spline in the scene view.");
            public static readonly string kEditSplineLabel = "Edit Spline";
        }

        int m_RectSelectionID = -1;

        Editor m_CurrentEditor;
        ShapeEditor m_ShapeEditor;
        RectSelectionTool m_RectSelectionTool = new RectSelectionTool();
        Spline m_Spline;
        UnityEngine.Object m_UndoObject;
        Vector3 m_LastPosition;
        Quaternion m_LastRotation;
        Vector3 m_LastScale;
        int m_LastHashCode;
        Bounds m_Bounds;

        public SplineSceneEditor(Spline spline, Editor editor, UnityEngine.Object undoObject)
        {
            m_Spline = spline;
            m_CurrentEditor = editor;
            m_UndoObject = undoObject;

            SetupSpriteShapeEditor();

            Undo.undoRedoPerformed += OnUndoRedo;
        }

        public void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
        }

        private void OnUndoRedo()
        {
            m_ShapeEditor.SetDirty();
            SceneView.RepaintAll();
        }

        public void OnInspectorGUI()
        {
            GUIContent editShapeIcon = Contents.editShapeIcon;

            if (EditorGUIUtility.isProSkin)
                editShapeIcon = Contents.editShapeIconPro;

            EditMode.DoEditModeInspectorModeButton(EditMode.SceneViewEditMode.Collider, Contents.kEditSplineLabel, editShapeIcon, GetBounds, m_CurrentEditor);
        }

        public void OnSceneGUI()
        {
            TransformChangedCheck();

            EditorGUI.BeginChangeCheck();

            m_ShapeEditor.OnGUI();
            DoRectSelectionGUI();

            if (EditorGUI.EndChangeCheck())
            {
                m_ShapeEditor.SetDirty();
                m_CurrentEditor.Repaint();
            }

            SplineHashCheck();

            if (Event.current.type == EventType.MouseMove)
                HandleUtility.Repaint();
        }

        private void DoRectSelectionGUI()
        {
            ISelection selection = ShapeEditorCache.GetSelection();

            if (m_RectSelectionID == -1)
                m_RectSelectionID = GUIUtility.GetControlID("RectSelection".GetHashCode(), FocusType.Passive);

            if (Event.current.GetTypeForControl(m_RectSelectionID) == EventType.MouseDown && Event.current.button == 0)
            {
                if (!Event.current.shift && !EditorGUI.actionKey)
                {
                    ShapeEditorCache.RecordUndo("Edit Selection");
                    ShapeEditorCache.ClearSelection();
                    GUI.changed = true;
                }
            }

            if (Event.current.GetTypeForControl(m_RectSelectionID) == EventType.MouseUp && Event.current.button == 0)
            {
                ShapeEditorCache.RecordUndo("Edit Selection");

                selection.EndSelection(true);
                m_ShapeEditor.HandleSinglePointSelection();

                GUI.changed = true;
            }

            EditorGUI.BeginChangeCheck();

            Rect selectionRect = m_RectSelectionTool.Do(m_RectSelectionID, (m_CurrentEditor.target as Component).transform.position);

            if (EditorGUI.EndChangeCheck())
            {
                selection.BeginSelection();

                for (int i = 0; i < m_Spline.GetPointCount(); ++i)
                {
                    if (selectionRect.Contains(HandleUtility.WorldToGUIPoint(LocalToWorld(m_Spline.GetPosition(i))), true))
                        selection.Select(i, true);
                }
            }
        }

        private void SplineHashCheck()
        {
            int hashCode = m_Spline.GetHashCode();

            if (m_LastHashCode != hashCode)
                m_ShapeEditor.SetDirty();

            m_LastHashCode = hashCode;
        }

        private void TransformChangedCheck()
        {
            Transform transform = GetTransform();
            if (m_LastPosition != transform.position || m_LastRotation != transform.rotation || m_LastScale != transform.lossyScale)
            {
                m_ShapeEditor.SetDirty();

                m_LastPosition = transform.position;
                m_LastRotation = transform.rotation;
                m_LastScale = transform.lossyScale;
            }
        }

        private Transform GetTransform()
        {
            return (m_CurrentEditor.target as Component).transform;
        }

        private Vector3 LocalToWorld(Vector3 position)
        {
            return GetTransform().TransformPoint(position);
        }

        private Vector3 WorldToLocal(Vector3 position)
        {
            return GetTransform().InverseTransformPoint(position);
        }

        private  void SetupSpriteShapeEditor()
        {
            m_ShapeEditor = new ShapeEditor()
            {
                //Data
                GetPosition = i => LocalToWorld(m_Spline.GetPosition(i)),
                SetPosition = (i, p) => m_Spline.SetPosition(i, WorldToLocal(p)),
                GetLeftTangent = i => LocalToWorld(m_Spline.GetLeftTangent(i) + m_Spline.GetPosition(i)) - LocalToWorld(m_Spline.GetPosition(i)),
                SetLeftTangent = (i, p) => m_Spline.SetLeftTangent(i, WorldToLocal(p + LocalToWorld(m_Spline.GetPosition(i))) - m_Spline.GetPosition(i)),
                GetRightTangent = i => LocalToWorld(m_Spline.GetRightTangent(i) + m_Spline.GetPosition(i)) - LocalToWorld(m_Spline.GetPosition(i)),
                SetRightTangent = (i, p) => m_Spline.SetRightTangent(i, WorldToLocal(p + LocalToWorld(m_Spline.GetPosition(i))) - m_Spline.GetPosition(i)),
                GetTangentMode = i => (ShapeEditor.TangentMode)m_Spline.GetTangentMode(i),
                SetTangentMode = (i, m) => m_Spline.SetTangentMode(i, (ShapeTangentMode)m),
                InsertPointAt = (i, p) => m_Spline.InsertPointAt(i, WorldToLocal(p)),
                RemovePointAt = i => m_Spline.RemovePointAt(i),
                GetPointCount = () => m_Spline.GetPointCount(),
                // Transforms
                ScreenToWorld = (p) => ScreenToWorld(p),
                LocalToWorldMatrix = () => Matrix4x4.identity,
                WorldToScreen = (p) => HandleUtility.WorldToGUIPoint(p),
                GetForwardVector = () => GetTransform().forward,
                GetUpVector = () => GetTransform().up,
                GetRightVector = () => GetTransform().right,
                // Other
                Snap = (p) => SnapPoint(p),
                RecordUndo = RecordUndo,
                OpenEnded = () => m_Spline.isOpenEnded,
                Repaint = m_CurrentEditor.Repaint
            };
        }

        public void CalculateBounds()
        {
            //Hack to disable framing on enter edit mode. We calculate bounds that are always visible by the camera.
            if (EditorWindow.focusedWindow == SceneView.currentDrawingSceneView)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(new Vector2(Camera.current.pixelWidth * 0.5f, Camera.current.pixelHeight * 0.5f));
                m_Bounds = new Bounds(ray.GetPoint(Camera.current.nearClipPlane), Vector3.one);
            }
        }

        private Bounds GetBounds()
        {
            return m_Bounds;
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition, Plane plane)
        {
            Ray ray = HandleUtility.GUIPointToWorldRay(screenPosition);

            float distance;
            plane.Raycast(ray, out distance);
            return ray.GetPoint(distance);
        }

        private Vector3 ScreenToWorld(Vector2 screenPosition)
        {
            Transform transform = (m_CurrentEditor.target as Component).transform;
            return ScreenToWorld(screenPosition, new Plane(transform.forward, transform.position));
        }

        private Vector3 SnapPoint(Vector3 position)
        {
            Vector2 np0screen = m_ShapeEditor.WorldToScreen(position);
            Vector2 snappedScreen = m_ShapeEditor.WorldToScreen(SnappingUtility.Snap(position));

            float snapDistance = (np0screen - snappedScreen).magnitude;
            if (snapDistance < 15f)
            {
                position = SnappingUtility.Snap(position);
            }
            return position;
        }

        private void RecordUndo()
        {
            Undo.RegisterCompleteObjectUndo(m_UndoObject, "Edit Spline");
        }
    }
}
