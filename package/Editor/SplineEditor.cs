using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityTexture2D = UnityEngine.Texture2D;

namespace UnityEditor.U2D
{
    public class SplineEditor
    {
        private static readonly int m_DragPointHashCode = "DragPoint".GetHashCode();
        private static readonly int m_DragEdgeHashCode = "DragEdge".GetHashCode();
        private static readonly int m_DragTangentHashCode = "DragTangent".GetHashCode();
        private static readonly int m_PointHashCode = "Point".GetHashCode();
        private static readonly int m_EdgeHashCode = "Edge".GetHashCode();
        private static readonly int m_TangentHashCode = "Tangent".GetHashCode();

        public enum TangentMode { Linear, Continuous, Broken }

        private class Styles
        {
            public readonly GUIStyle pointNormalStyle;
            public readonly GUIStyle pointHoveredStyle;
            public readonly GUIStyle pointSelectedStyle;
            public readonly GUIStyle pointPreviewStyle;
            public readonly GUIStyle tangentNormalStyle;
            public readonly GUIStyle tangentHoveredStyle;

            public Styles()
            {
                Texture2D pointNormal = Resources.Load<Texture2D>("ss_pointNormal");
                Texture2D pointHovered = Resources.Load<Texture2D>("ss_pointHovered");
                Texture2D pointSelected = Resources.Load<Texture2D>("ss_pointSelected");
                Texture2D pointPreview = Resources.Load<Texture2D>("ss_pointPreview");
                Texture2D tangentNormal = Resources.Load<Texture2D>("ss_tangentNormal");

                pointNormalStyle = new GUIStyle();
                pointNormalStyle.normal.background = pointNormal;
                pointNormalStyle.fixedWidth = 12f;
                pointNormalStyle.fixedHeight = 12f;

                pointHoveredStyle = new GUIStyle();
                pointHoveredStyle.normal.background = pointHovered;
                pointHoveredStyle.fixedWidth = 12f;
                pointHoveredStyle.fixedHeight = 12f;

                pointSelectedStyle = new GUIStyle();
                pointSelectedStyle.normal.background = pointSelected;
                pointSelectedStyle.fixedWidth = 12f;
                pointSelectedStyle.fixedHeight = 12f;

                pointPreviewStyle = new GUIStyle();
                pointPreviewStyle.normal.background = pointPreview;
                pointPreviewStyle.fixedWidth = 12f;
                pointPreviewStyle.fixedHeight = 12f;

                tangentNormalStyle = new GUIStyle();
                tangentNormalStyle.normal.background = tangentNormal;
                tangentNormalStyle.fixedWidth = 8f;
                tangentNormalStyle.fixedHeight = 8f;

                tangentHoveredStyle = new GUIStyle();
                tangentHoveredStyle.normal.background = pointHovered;
                tangentHoveredStyle.fixedWidth = 10f;
                tangentHoveredStyle.fixedHeight = 10f;
            }
        }

        private Styles m_Styles;
        private Styles styles
        {
            get
            {
                if (m_Styles == null)
                    m_Styles = new Styles();

                return m_Styles;
            }
        }

        public Func<int, Vector3> GetLocalPosition = i => Vector3.zero;
        public Func<int, Vector3> GetPosition = i => Vector3.zero;
        public Action<int, Vector3> SetPosition = (i, p) => { };
        public Func<int, Vector3> GetLeftTangent = i => Vector3.zero;
        public Action<int, Vector3> SetLeftTangent = (i, p) => { };
        public Func<int, Vector3> GetRightTangent = i => Vector3.zero;
        public Action<int, Vector3> SetRightTangent = (i, p) => { };
        public Func<int, TangentMode> GetTangentMode = i => TangentMode.Linear;
        public Action<int, TangentMode> SetTangentMode = (i, m) => { };
        public Action<int, Vector3> InsertPointAt = (i, p) => { };
        public Action<int> RemovePointAt = i => { };
        public Func<int> GetPointCount = () => 0;
        // --- Transforms
        public Func<Vector2, Vector3> ScreenToWorld = i => i;
        public Func<Vector3, Vector2> WorldToScreen = i => i;
        public Func<Matrix4x4> LocalToWorldMatrix = () => Matrix4x4.identity;
        public Func<Vector3> GetForwardVector = () => Vector3.forward;
        public Func<Vector3> GetUpVector = () => Vector3.up;
        public Func<Vector3> GetRightVector = () => Vector3.right;
        // --- Other
        public Action Repaint = () => { };
        public Action RegisterUndo = () => { };
        public Func<Vector3, Vector3> Snap = i => i;
        public Action<Bounds> Frame = b => { };
        public Func<bool> OpenEnded = () => false;
        public Func<Vector3, float> GetHandleSize;
        public Handles.CapFunction GetPointCapNormal;
        public Handles.CapFunction GetPointCapHovered;
        public Handles.CapFunction GetPointCapSelected;
        public Handles.CapFunction GetPointCapActive;
        public Handles.CapFunction GetPointCapPreview;
        public Handles.CapFunction GetTangentCapNormal;
        public Handles.CapFunction GetTangentCapHovered;
        public Handles.CapFunction GetTangentCapActive;
        public Func<int, int> GetSpriteIndex = i => 0;
        public Action<int, int> SetSpriteIndex = (s, i) => { };
        // --- END

        public Texture2D lineTexture { get; set; }

        int m_ActiveEdgeIndex = -1;
        Vector3 m_SliderPosition;
        Vector3 m_ClosestPoint;
        float m_ClosestPointT;
        int m_DragPointControlId = -1;
        int m_DragEdgeControlId = -1;
        int m_DragTangentControlId = -1;
        int m_ActiveTangentPointIndex = -1;
        bool m_DragLeftTangent = false;
        int m_ActivePointIndex = -1;
        int m_HoveredPointIndex = -1;
        int m_HoveredEdgeIndex = -1;
        int m_HoveredTangentPoint = -1;
        bool m_HoveredLeftTangent = false;
        bool m_EdgePointsDirty = true;

        private List<Vector3[]> m_EdgePoints = new List<Vector3[]>();

        private static readonly Color kTangentColor = Handles.selectedColor;
        private static readonly Color kTangentColorAlternative = Handles.selectedColor;
        private const float kEdgeWidth = 2f;
        private const float kActiveEdgeWidth = 6f;
        private const int kBezierPatch = 40;

        private Event currentEvent { get; set; }
        private bool m_SliderChanged = false;
        private TangentMode m_CurrentTangentMode = TangentMode.Linear;

        public SplineEditor()
        {
            GetHandleSize = GetDefaultHandleSizeForPoint;
            GetPointCapNormal = PointCapNormal;
            GetPointCapHovered = PointCapHovered;
            GetPointCapSelected = PointCapSelected;
            GetPointCapActive = PointCapHovered;
            GetPointCapPreview = PointCapPreview;
            GetTangentCapNormal = TangentCapNormal;
            GetTangentCapHovered = TangentCapHovered;
            GetTangentCapActive = TangentCapHovered;
        }

        public void SetDirty()
        {
            m_EdgePointsDirty = true;
        }

        public void OnGUI()
        {
            Color currentColor = Handles.color;
            Matrix4x4 currentMatrix = Handles.matrix;
            Handles.matrix = LocalToWorldMatrix();

            currentEvent = Event.current;

            m_DragPointControlId = GUIUtility.GetControlID(m_DragPointHashCode, FocusType.Passive);
            m_DragEdgeControlId = GUIUtility.GetControlID(m_DragEdgeHashCode, FocusType.Passive);
            m_DragTangentControlId = GUIUtility.GetControlID(m_DragTangentHashCode, FocusType.Passive);

            if (currentEvent.type == EventType.Layout)
            {
                m_HoveredPointIndex = -1;
                m_HoveredEdgeIndex = -1;
                m_HoveredTangentPoint = -1;
            }

            SetupSliderState();

            LayoutPoints();
            LayoutTangents();
            LayoutEdges();

            DrawEdges();
            DrawPoints();
            DrawCreatePointPreview();
            DrawTangents();

            HandleDragEdge();
            HandleDragTangent();
            HandleInsertPointToEdge();
            HandleSelectPoint();
            HandleDragPoints();
            HandleDeletePoints();
            HandleUnselect();
            Framing();

            Handles.color = currentColor;
            Handles.matrix = currentMatrix;
        }

        private void SetupSliderState()
        {
            if (GUIUtility.hotControl == 0)
                m_SliderChanged = false;
        }

        private void RegisterUndoOnSliderChangedOnce()
        {
            Debug.Assert(GUIUtility.hotControl != 0);

            if (m_SliderChanged == false)
            {
                m_SliderChanged = true;
                RegisterUndo();
                SplineEditorCache.RigisterUndo();
            }
        }

        private void PrepareEdgePoints()
        {
            int pointCount = GetPointCount();
            int edgeCount = OpenEnded() ? pointCount - 1 : pointCount;

            if (m_EdgePoints.Count != edgeCount)
                m_EdgePointsDirty = true;

            if (!m_EdgePointsDirty)
                return;

            m_EdgePoints.Clear();

            for (int index = 0; index < edgeCount; ++index)
            {
                int nextIndex = SplineUtility.NextIndex(index, pointCount);

                Vector3 position0 = GetPosition(index);
                Vector3 position1 = GetPosition(nextIndex);

                if (GetTangentMode(index) == TangentMode.Linear && GetTangentMode(nextIndex) == TangentMode.Linear)
                {
                    m_EdgePoints.Add(new Vector3[] { position0, position1 });
                }
                else
                {
                    var tangent0 = GetRightTangent(index) + position0;
                    var tangent1 = GetLeftTangent(nextIndex) + position1;
                    m_EdgePoints.Add(Handles.MakeBezierPoints(position0, position1, tangent0, tangent1, kBezierPatch));
                }
            }

            m_EdgePointsDirty = false;
        }

        private void LayoutPoints()
        {
            for (int index = 0; index < GetPointCount(); index++)
            {
                int id = GUIUtility.GetControlID(m_PointHashCode, FocusType.Passive);

                if (currentEvent.type == EventType.Layout)
                {
                    Vector3 position = GetPosition(index);
                    GetPointCapNormal(id, position, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(position), EventType.Layout);

                    if (m_HoveredEdgeIndex == -1)
                    {
                        if (HandleUtility.nearestControl == id)
                        {
                            m_HoveredPointIndex = index;
                        }
                    }
                }
            }
        }

        private void DrawPoints()
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            for (int index = 0; index < GetPointCount(); index++)
            {
                var position = GetPosition(index);

                if (m_ActivePointIndex == index)
                    GetPointCapActive(0, position, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(position), currentEvent.type);
                else if (m_HoveredPointIndex == index && GUIUtility.hotControl == 0)
                    GetPointCapHovered(0, position, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(position), currentEvent.type);
                else if (SplineEditorCache.GetSelection().IsSelected(index))
                    GetPointCapSelected(0, position, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(position), currentEvent.type);
                else
                    GetPointCapNormal(0, position, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(position), currentEvent.type);
            }
        }

        private void DrawCreatePointPreview()
        {
            if (GUIUtility.hotControl == 0 && m_HoveredTangentPoint == -1 && m_HoveredPointIndex == -1 && !currentEvent.shift && !EdgeDragModifiersActive())
            {
                if (m_HoveredEdgeIndex != -1)
                    GetPointCapHovered(0, m_ClosestPoint, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(m_ClosestPoint), currentEvent.type);
                else
                    GetPointCapPreview(0, m_ClosestPoint, Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(m_ClosestPoint), currentEvent.type);
            }
        }

        private void HandleSelectPoint()
        {
            EventType eventType = currentEvent.GetTypeForControl(m_DragPointControlId);

            if (GUIUtility.hotControl == 0 && m_HoveredPointIndex != -1 && eventType == EventType.MouseDown && currentEvent.button == 0)
            {
                SelectPoint(m_HoveredPointIndex);
            }
        }

        private void HandleDeletePoints()
        {
            bool wantsDelete = GUIUtility.hotControl == 0 &&
                (currentEvent.type == EventType.ExecuteCommand || currentEvent.type == EventType.ValidateCommand)
                && (currentEvent.commandName == "SoftDelete" || currentEvent.commandName == "Delete");

            if (wantsDelete)
            {
                if (currentEvent.type == EventType.ValidateCommand)
                {
                    currentEvent.Use();
                }
                else if (currentEvent.type == EventType.ExecuteCommand)
                {
                    DeleteSelected();
                    currentEvent.Use();
                }
            }
        }

        private void HandleDragPoints()
        {
            EventType eventType = currentEvent.GetTypeForControl(m_DragPointControlId);

            if ((!currentEvent.alt && GUIUtility.hotControl == 0 && m_HoveredPointIndex != -1 && !EditorGUI.actionKey) ||
                GUIUtility.hotControl == m_DragPointControlId)
            {
                if (eventType == EventType.Layout && GUIUtility.hotControl == 0)
                    GetPointCapNormal(m_DragPointControlId, GetPosition(m_HoveredPointIndex), Quaternion.LookRotation(GetForwardVector(), GetUpVector()), GetHandleSize(GetPosition(m_HoveredPointIndex)), EventType.Layout);

                if (eventType == EventType.MouseDown && currentEvent.button == 0)
                {
                    m_ActivePointIndex = m_HoveredPointIndex;
                    m_SliderPosition = GetPosition(m_HoveredPointIndex);
                }

                if (eventType == EventType.MouseUp && currentEvent.button == 0)
                    m_ActivePointIndex = -1;

                EditorGUI.BeginChangeCheck();

                Vector3 newPosition = DoSlider(m_DragPointControlId, m_SliderPosition, GetUpVector(), GetRightVector(), GetHandleSize(m_SliderPosition), (int cid, Vector3 p, Quaternion q, float s, EventType et) => { });

                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoOnSliderChangedOnce();

                    newPosition = Snap(newPosition);
                    MoveSelectedPoints(newPosition - m_SliderPosition);
                }

                m_SliderPosition = newPosition;
            }
        }

        private void LayoutEdges()
        {
            if (currentEvent.type == EventType.Layout)
                PrepareEdgePoints();    

            int pointCount = GetPointCount();
            int edgeCount = OpenEnded() ? pointCount - 1 : pointCount;

            float minDistance = float.MaxValue;

            for (int index = 0; index < edgeCount; ++index)
            {
                int id = GUIUtility.GetControlID(m_EdgeHashCode, FocusType.Passive);

                if (currentEvent.type == EventType.Layout)
                {
                    int nextIndex = SplineUtility.NextIndex(index, pointCount);

                    Vector3 position0 = GetPosition(index);
                    Vector3 position1 = GetPosition(nextIndex);
                    Vector3 tangent0 = GetRightTangent(index) + position0;
                    Vector3 tangent1 = GetLeftTangent(nextIndex) + position1;

                    float t;
                    Vector3 closestPoint = BezierUtility.ClosestPointOnCurve(ScreenToWorld(currentEvent.mousePosition), position0, position1, tangent0, tangent1, out t);

                    Vector2 guiPosition = HandleUtility.WorldToGUIPoint(closestPoint);

                    float distance = (currentEvent.mousePosition - guiPosition).magnitude;

                    if (m_HoveredPointIndex == -1)
                    {
                        HandleUtility.AddControl(id, distance);

                        if (HandleUtility.nearestControl == id)
                        {
                            m_HoveredEdgeIndex = index;
                            m_HoveredPointIndex = -1;
                            m_HoveredTangentPoint = -1;
                        }
                    }

                    if (distance < minDistance)
                    {
                        m_ClosestPoint = closestPoint;
                        m_ClosestPointT = t;
                        minDistance = distance;
                    }
                }
            }
        }

        private void DrawEdges()
        {
            PrepareEdgePoints();

            if (currentEvent.type != EventType.Repaint)
                return;

            Color handlesOldColor = Handles.color;

            var pointCount = GetPointCount();
            int edgeCount = OpenEnded() ? pointCount - 1 : pointCount;

            for (int index = 0; index < edgeCount; index++)
            {
                Color edgeColor = Color.white;

                if ((m_HoveredEdgeIndex == index && HandleUtility.nearestControl == m_DragEdgeControlId) ||
                    (m_ActiveEdgeIndex == index && GUIUtility.hotControl == m_DragEdgeControlId))
                    edgeColor = Handles.selectedColor;

                float edgeWidth = kEdgeWidth;

                if ((m_ActiveEdgeIndex == index && GUIUtility.hotControl == m_DragEdgeControlId) ||
                    (GUIUtility.hotControl == 0 && m_HoveredEdgeIndex == index))
                    edgeWidth = kActiveEdgeWidth;

                Handles.color = edgeColor;
                Handles.DrawAAPolyLine(lineTexture, edgeWidth, m_EdgePoints[index]);
            }
            Handles.color = handlesOldColor;
        }

        private void HandleUnselect()
        {
            EventType eventType = currentEvent.type;

            if (GUIUtility.hotControl == 0 && eventType == EventType.MouseDown && currentEvent.button == 1)
            {
                ClearSelection();
            }
        }

        private void HandleInsertPointToEdge()
        {
            EventType eventType = currentEvent.GetTypeForControl(m_DragPointControlId);

            if (m_HoveredPointIndex == -1 && m_HoveredEdgeIndex != -1 && GUIUtility.hotControl == 0 && eventType == EventType.MouseDown && currentEvent.button == 0 && !currentEvent.shift)
            {
                RegisterUndo();
                ClearSelection();

                int nextIndex = SplineUtility.NextIndex(m_HoveredEdgeIndex, GetPointCount());

                TangentMode leftTangentMode = GetTangentMode(m_HoveredEdgeIndex);
                TangentMode rightTangentMode = GetTangentMode(nextIndex);

                Vector3 leftStartPosition;
                Vector3 leftEndPosition;
                Vector3 leftStartTangent;
                Vector3 leftEndTangent;

                Vector3 rightStartPosition;
                Vector3 rightEndPosition;
                Vector3 rightStartTangent;
                Vector3 rightEndTangent;

                Vector3 position0 = GetPosition(m_HoveredEdgeIndex);
                Vector3 position1 = GetPosition(nextIndex);
                Vector3 tangent0 = GetRightTangent(m_HoveredEdgeIndex) + position0;
                Vector3 tangent1 = GetLeftTangent(nextIndex) + position1;

                BezierUtility.SplitBezier(m_ClosestPointT, position0, position1, tangent0, tangent1,
                    out leftStartPosition, out leftEndPosition, out leftStartTangent, out leftEndTangent,
                    out rightStartPosition, out rightEndPosition, out rightStartTangent, out rightEndTangent);

                if (leftTangentMode != TangentMode.Linear)
                    SetRightTangent(m_HoveredEdgeIndex, leftStartTangent - leftStartPosition);

                if (rightTangentMode != TangentMode.Linear)
                    SetLeftTangent(nextIndex, rightEndTangent - rightEndPosition);

                InsertPointAt(nextIndex, m_ClosestPoint);

                if (leftTangentMode != TangentMode.Linear || rightTangentMode != TangentMode.Linear)
                {
                    if (leftTangentMode == TangentMode.Linear && rightTangentMode == TangentMode.Linear)
                        SetTangentModeUseThisOne(nextIndex, TangentMode.Linear);
                    else
                        SetTangentModeUseThisOne(nextIndex, TangentMode.Continuous);

                    SetLeftTangent(nextIndex, leftEndTangent - leftEndPosition);
                    SetRightTangent(nextIndex, rightStartTangent - rightStartPosition);
                }

                m_HoveredPointIndex = nextIndex;
                m_HoveredEdgeIndex = -1;

                HandleUtility.nearestControl = m_DragPointControlId;
            }
        }

        private void HandleDragEdge()
        {
            EventType eventType = currentEvent.GetTypeForControl(m_DragEdgeControlId);

            if ((m_HoveredEdgeIndex != -1 && GUIUtility.hotControl == 0 && EdgeDragModifiersActive()) ||
                GUIUtility.hotControl == m_DragEdgeControlId)
            {
                if (eventType == EventType.MouseDown && currentEvent.button == 0)
                {
                    m_ActiveEdgeIndex = m_HoveredEdgeIndex;
                    m_SliderPosition = GetPosition(m_HoveredEdgeIndex);
                }

                if (eventType == EventType.MouseUp && currentEvent.button == 0)
                {
                    m_ActiveEdgeIndex = -1;
                }

                if (eventType == EventType.Layout && GUIUtility.hotControl == 0)
                    HandleUtility.AddControl(m_DragEdgeControlId, 0f);

                EditorGUI.BeginChangeCheck();

                var newPosition = DoSlider(m_DragEdgeControlId, m_SliderPosition, GetUpVector(), GetRightVector(), GetHandleSize(m_SliderPosition), (int cid, Vector3 p, Quaternion q, float s, EventType et) => { });

                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoOnSliderChangedOnce();

                    var snappedDelta = Snap(newPosition) - m_SliderPosition;
                    var nextIndex = SplineUtility.NextIndex(m_ActiveEdgeIndex, GetPointCount());

                    SetPosition(m_ActiveEdgeIndex, GetPosition(m_ActiveEdgeIndex) + snappedDelta);
                    SetPosition(nextIndex, GetPosition(nextIndex) + snappedDelta);

                    m_EdgePointsDirty = true;
                }

                m_SliderPosition = newPosition;
            }
        }

        private void LayoutTangents()
        {
            ISelection selection = SplineEditorCache.GetSelection();

            int selectedPoint = selection.single;

            if (selectedPoint == -1 || GetTangentMode(selectedPoint) == TangentMode.Linear)
                return;

            int tangentId = GUIUtility.GetControlID(m_TangentHashCode, FocusType.Passive);

            Vector3 position = GetPosition(selectedPoint);

            if (currentEvent.type == EventType.Layout)
            {
                Vector3 leftTangentPosition = GetLeftTangent(selectedPoint);
                GetTangentCapNormal(tangentId, leftTangentPosition + position, Quaternion.identity, GetHandleSize(leftTangentPosition + position), EventType.Layout);

                if (HandleUtility.nearestControl == tangentId)
                {
                    m_HoveredTangentPoint = selectedPoint;
                    m_HoveredLeftTangent = true;
                    m_HoveredPointIndex = -1;
                }
            }

            tangentId = GUIUtility.GetControlID(m_TangentHashCode, FocusType.Passive);

            if (currentEvent.type == EventType.Layout)
            {
                Vector3 rightTangentPosition = GetRightTangent(selection.single);
                GetTangentCapNormal(tangentId, rightTangentPosition + position, Quaternion.identity, GetHandleSize(rightTangentPosition + position), EventType.Layout);

                if (HandleUtility.nearestControl == tangentId)
                {
                    m_HoveredTangentPoint = selectedPoint;
                    m_HoveredLeftTangent = false;
                    m_HoveredPointIndex = -1;
                }
            }
        }

        private void DrawTangentLine(Vector3 position, Vector3 tangent, Color color)
        {
            if (currentEvent.type != EventType.Repaint)
                return;

            Handles.color = color;
            Handles.DrawAAPolyLine(lineTexture, 3f, new Vector3[] { position, position + tangent });
        }

        private void DrawTangents()
        {
            ISelection selection = SplineEditorCache.GetSelection();

            int selectedPoint = selection.single;

            if (selectedPoint == -1 || GetTangentMode(selectedPoint) == TangentMode.Linear || currentEvent.type != EventType.Repaint)
                return;

            Vector3 position = GetPosition(selectedPoint);
            Vector3 leftTangent = GetLeftTangent(selectedPoint);
            Vector3 rightTangent = GetRightTangent(selection.single);

            Color color = kTangentColor;

            DrawTangentLine(position, leftTangent, color);

            if (GetTangentMode(selectedPoint) == TangentMode.Broken)
                color = kTangentColorAlternative;

            DrawTangentLine(position, rightTangent, color);

            if (m_ActiveTangentPointIndex != -1)
            {
                if (m_DragLeftTangent)
                {
                    GetTangentCapActive(0, leftTangent + position, Quaternion.identity, GetHandleSize(leftTangent + position), EventType.Repaint);
                    GetTangentCapNormal(0, rightTangent + position, Quaternion.identity, GetHandleSize(rightTangent + position), EventType.Repaint);
                }
                else
                {
                    GetTangentCapNormal(0, leftTangent + position, Quaternion.identity, GetHandleSize(leftTangent + position), EventType.Repaint);
                    GetTangentCapActive(0, rightTangent + position, Quaternion.identity, GetHandleSize(rightTangent + position), EventType.Repaint);
                }
            }
            else if (GUIUtility.hotControl == 0 && m_HoveredTangentPoint != -1)
            {
                if (m_HoveredLeftTangent)
                {
                    GetTangentCapHovered(0, leftTangent + position, Quaternion.identity, GetHandleSize(leftTangent + position), EventType.Repaint);
                    GetTangentCapNormal(0, rightTangent + position, Quaternion.identity, GetHandleSize(rightTangent + position), EventType.Repaint);
                }
                else
                {
                    GetTangentCapNormal(0, leftTangent + position, Quaternion.identity, GetHandleSize(leftTangent + position), EventType.Repaint);
                    GetTangentCapHovered(0, rightTangent + position, Quaternion.identity, GetHandleSize(rightTangent + position), EventType.Repaint);
                }
            }
            else
            {
                GetTangentCapNormal(0, leftTangent + position, Quaternion.identity, GetHandleSize(leftTangent + position), EventType.Repaint);
                GetTangentCapNormal(0, rightTangent + position, Quaternion.identity, GetHandleSize(rightTangent + position), EventType.Repaint);
            }
        }

        private void HandleDragTangent()
        {
            EventType eventType = currentEvent.GetTypeForControl(m_DragTangentControlId);

            if ((!currentEvent.alt && GUIUtility.hotControl == 0 && m_HoveredTangentPoint != -1 && !EditorGUI.actionKey) ||
                GUIUtility.hotControl == m_DragTangentControlId)
            {
                if (eventType == EventType.MouseDown && currentEvent.button == 0)
                {
                    m_ActiveTangentPointIndex = m_HoveredTangentPoint;
                    m_DragLeftTangent = m_HoveredLeftTangent;
                    m_CurrentTangentMode = GetTangentMode(m_ActiveTangentPointIndex);

                    if (m_DragLeftTangent)
                        m_SliderPosition = GetLeftTangent(m_HoveredTangentPoint) + GetPosition(m_HoveredTangentPoint);
                    else
                        m_SliderPosition = GetRightTangent(m_HoveredTangentPoint) + GetPosition(m_HoveredTangentPoint);
                }

                var abortDrag = 
                    GUIUtility.hotControl == m_DragTangentControlId &&
                    m_ActiveTangentPointIndex != -1 &&
                    m_CurrentTangentMode != GetTangentMode(m_ActiveTangentPointIndex);

                var finishDrag =
                    GUIUtility.hotControl == m_DragTangentControlId &&
                    eventType == EventType.MouseUp &&
                    currentEvent.button == 0;

                if (finishDrag || abortDrag)
                {
                    Debug.Assert(m_ActiveTangentPointIndex != -1);

                    var leftTangent = GetLeftTangent(m_ActiveTangentPointIndex);
                    var rightTangent = GetRightTangent(m_ActiveTangentPointIndex);

                    if (leftTangent.sqrMagnitude == 0f && rightTangent.sqrMagnitude == 0f)
                        SetTangentModeUseThisOne(m_ActiveTangentPointIndex, TangentMode.Linear);

                    m_ActiveTangentPointIndex = -1;
                }

                if (abortDrag)
                    GUIUtility.hotControl = 0;

                if (eventType == EventType.Layout && GUIUtility.hotControl == 0)
                    HandleUtility.AddControl(m_DragTangentControlId, 0f);

                EditorGUI.BeginChangeCheck();

                Vector3 newPosition = DoSlider(m_DragTangentControlId, m_SliderPosition, GetUpVector(), GetRightVector(), GetHandleSize(m_SliderPosition), (int cid, Vector3 p, Quaternion q, float s, EventType et) => { });

                if (EditorGUI.EndChangeCheck())
                {
                    RegisterUndoOnSliderChangedOnce();

                    var tangent = newPosition - GetPosition(m_ActiveTangentPointIndex);

                    if (tangent.magnitude < GetHandleSize(GetPosition(m_ActiveTangentPointIndex)))
                        tangent = Vector3.zero;

                    var activeMode = GetTangentMode(m_ActiveTangentPointIndex);
                    var doMirrorTangents = currentEvent.shift && activeMode != TangentMode.Linear;

                    if (m_DragLeftTangent)
                    {
                        SetLeftTangent(m_ActiveTangentPointIndex, tangent);

                        if (doMirrorTangents)
                        {
                            tangent *= -1f; 
                            SetRightTangent(m_ActiveTangentPointIndex, tangent);
                        }
                    }
                    else
                    {
                        SetRightTangent(m_ActiveTangentPointIndex, tangent);

                        if (doMirrorTangents)
                        {
                            tangent *= -1f;
                            SetLeftTangent(m_ActiveTangentPointIndex, tangent);
                        }
                    }

                    ValidateTangents(!m_DragLeftTangent);
                }
            }
        }

        private void Framing()
        {
            if (GetPointCount() == 0)
                return;

            ISelection selection = SplineEditorCache.GetSelection();

            if (currentEvent.commandName == "FrameSelected" && selection.Count > 0)
            {
                switch (currentEvent.type)
                {
                    case EventType.ExecuteCommand:
                        Bounds bounds = default(Bounds);

                        if (selection.Count == 0)
                        {
                            bounds = new Bounds(GetPosition(0), Vector3.zero);

                            for (int index = 1; index < GetPointCount(); ++index)
                            {
                                bounds.Encapsulate(GetPosition(index));
                            }
                        }
                        else
                        {
                            bounds = new Bounds(GetPosition(selection.any), Vector3.zero);

                            foreach (int index in selection)
                                bounds.Encapsulate(GetPosition(index));
                        }

                        Frame(bounds);

                        currentEvent.Use();
                        break;

                    case EventType.ValidateCommand:
                        currentEvent.Use();
                        break;
                }
            }
        }

        private float GetDefaultHandleSizeForPoint(Vector3 position)
        {
            return Camera.current != null ? HandleUtility.GetHandleSize(position) * 0.09f : 5f;
        }

        private float GetTangentSizeForPoint(int index)
        {
            return GetHandleSize(GetPosition(index)) * 0.7f;
        }

        private void ValidateTangents(bool rightTangentChanged)
        {
            var selection = SplineEditorCache.GetSelection();
            var mode = GetTangentMode(selection.single);
            var leftTangent = GetLeftTangent(selection.single);
            var rightTangent = GetRightTangent(selection.single);

            if (mode == TangentMode.Continuous)
            {
                if (rightTangentChanged && rightTangent.magnitude > 0)
                {
                    var len = leftTangent.magnitude;
                    var rta = rightTangent.normalized;
                    leftTangent = -rta * len;
                }
                else if (leftTangent.magnitude > 0)
                {
                    var len = rightTangent.magnitude;
                    var lta = leftTangent.normalized;
                    rightTangent = -lta * len;
                }
            }

            SetLeftTangent(selection.single, leftTangent);
            SetRightTangent(selection.single, rightTangent);

            SplineEditorCache.instance.rightTangentChanged = rightTangentChanged;

            UpdateTangentCache();
        }

        private bool EdgeDragModifiersActive()
        {
            return currentEvent.modifiers == EventModifiers.Control || currentEvent.modifiers == EventModifiers.Command;
        }

        private static Vector3 DoSlider(int id, Vector3 position, Vector3 slide1, Vector3 slide2, float s, Handles.CapFunction cap)
        {
            return Handles.Slider2D(id, position, Vector3.zero, Vector3.Cross(slide1, slide2), slide1, slide2, s, cap, Vector2.zero, false);
        }

        private void ClearSelection()
        {
            SplineEditorCache.RigisterUndo("Edit Selection");
            SplineEditorCache.ClearSelection();

            GUI.changed = true;
        }

        private void DeleteSelected()
        {
            ISelection selection = SplineEditorCache.GetSelection();

            if (GetPointCount() <= 2)
                return;

            RegisterUndo();

            List<int> indices = new List<int>(selection);
            indices.Sort();

            for (int i = indices.Count - 1; i >= 0; --i)
                RemovePointAt(indices[i]);

            ClearSelection();

            GUI.changed = true;
        }

        private void MoveSelectedPoints(Vector3 delta)
        {
            ISelection selection = SplineEditorCache.GetSelection();

            RegisterUndo();

            if (delta.sqrMagnitude < float.Epsilon)
                return;

            foreach (int index in selection)
                SetPosition(index, GetPosition(index) + delta);
        }

        private void SelectPoint(int index)
        {
            ISelection selection = SplineEditorCache.GetSelection();

            bool additive = currentEvent.shift;
            bool subtractive = EditorGUI.actionKey;

            SplineEditorCache.RigisterUndo("Edit Selection");

            if (!additive && !subtractive)
                SplineEditorCache.ClearSelection();

            selection.Select(index, (!selection.IsSelected(index) || additive) && !subtractive);

            GUI.changed = true;
        }

        public void UpdateTangentCache()
        {
            for (var index = 0; index < GetPointCount(); ++index)
            {
                var mode = GetTangentMode(index);
                var leftTangent = GetLeftTangent(index);
                var rightTangent = GetRightTangent(index);

                if (mode == TangentMode.Continuous)
                {
                    SplineEditorCache.instance.SetLeftTangentContinous(index, leftTangent);
                    SplineEditorCache.instance.SetRightTangentContinous(index, rightTangent);
                }
                else if (mode == TangentMode.Broken)
                {
                    SplineEditorCache.instance.SetLeftTangentBroken(index, leftTangent);
                    SplineEditorCache.instance.SetRightTangentBroken(index, rightTangent);
                }
            }
        }

        public void PointCapNormal(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointNormalStyle);
        }

        public void PointCapHovered(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointHoveredStyle);
        }

        public void PointCapSelected(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointSelectedStyle);
        }

        public void PointCapPreview(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointPreviewStyle);
        }

        public void DrawGUIStyleCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle guiStyle)
        {
            if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0f)
                return;

            Handles.BeginGUI();
            guiStyle.Draw(GetGUIStyleRect(guiStyle, position), GUIContent.none, controlID);
            Handles.EndGUI();
        }

        public void TangentCapNormal(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.tangentNormalStyle);
        }

        public void TangentCapHovered(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.tangentHoveredStyle);
        }

        public void TangentCapActive(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            if (eventType == EventType.Layout)
                HandleUtility.AddControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            else if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.tangentHoveredStyle);
        }

        private Rect GetGUIStyleRect(GUIStyle style, Vector3 position)
        {
            Vector2 vector = HandleUtility.WorldToGUIPoint(position);

            float fixedWidth = style.fixedWidth;
            float fixedHeight = style.fixedHeight;

            return new Rect(vector.x - fixedWidth / 2f, vector.y - fixedHeight / 2f - 2f, fixedWidth, fixedHeight);
        }

        public void CycleTangentMode()
        {
            var selection = SplineEditorCache.GetSelection();
            if (selection.single == -1)
                return;

            RegisterUndo();

            var oldMode = GetTangentMode(selection.single);
            var newMode = GetNextTangentMode(oldMode);

            SetTangentModeUseThisOne(selection.single, newMode);
        }

        private TangentMode GetNextTangentMode(TangentMode current)
        {
            return (TangentMode)((((int)current) + 1) % Enum.GetValues(typeof(TangentMode)).Length);
        }

        static float SlopeAngle(Vector2 start, Vector2 end)
        {
            Vector2 dir = start - end;
            dir.Normalize();
            Vector2 dvup = new Vector2(0, 1f);
            Vector2 dvrt = new Vector2(1f, 0);

            float dr = Vector2.Dot(dir, dvrt);
            float du = Vector2.Dot(dir, dvup);
            float cu = Mathf.Acos(du);
            float sn = dr >= 0 ? 1.0f : -1.0f;
            float an = cu * Mathf.Rad2Deg * sn;

            // Adjust angles when direction is parallel to Up Axis.
            an = (du != 1f) ? an : 0;
            an = (du != -1f) ? an : -180f;
            return an;
        }

        public void CycleSpriteIndex()
        {
            var selection = SplineEditorCache.GetSelection();
            var spriteShape = SplineEditorCache.GetTarget().spriteShape;
            if (selection.single == -1 || spriteShape == null)
                return;

            Debug.Assert(SplineEditorCache.GetTarget() != null);

            var nextIndex = SplineUtility.NextIndex(selection.single, GetPointCount());
            var pos1 = GetLocalPosition(selection.single);
            var pos2 = GetLocalPosition(nextIndex);
            var angle = SlopeAngle(pos1, pos2) + 90;
            var angleRangeIndex = SpriteShapeEditorUtility.GetRangeIndexFromAngle(spriteShape, angle);
            if (angleRangeIndex == -1)
                return;

            var angleRange = spriteShape.angleRanges[angleRangeIndex];
            var spriteIndex = 0;
            if (angleRange.sprites.Count > 0)
                spriteIndex = (GetSpriteIndex(selection.single) + 1) % angleRange.sprites.Count;

            RegisterUndo();

            SetSpriteIndex(selection.single, spriteIndex);

            SetDirty();
        }

        public void SetTangentModeUseThisOne(int pointIndex, TangentMode mode)
        {
            SetTangentMode(pointIndex, mode);

            if (SplineEditorCache.IsValid() && SplineEditorCache.GetTarget() != null)
            {
                if (mode == TangentMode.Continuous)
                {
                    SetLeftTangent(pointIndex, SplineEditorCache.instance.GetLeftTangentContinous(pointIndex));
                    SetRightTangent(pointIndex, SplineEditorCache.instance.GetRightTangentContinous(pointIndex));
                }
                if (mode == TangentMode.Broken)
                {
                    SetLeftTangent(pointIndex, SplineEditorCache.instance.GetLeftTangentBroken(pointIndex));
                    SetRightTangent(pointIndex, SplineEditorCache.instance.GetRightTangentBroken(pointIndex));
                }
            }

            if (mode == TangentMode.Continuous || mode == TangentMode.Broken)
            {
                if (GetLeftTangent(pointIndex).sqrMagnitude == 0f && GetRightTangent(pointIndex).sqrMagnitude == 0f)
                    ResetTangents(pointIndex);
            }

            SetDirty();
        }

        private void ResetTangents(int pointIndex)
        {
            Vector3 position = GetPosition(pointIndex);
            Vector3 positionNext = GetPosition(SplineUtility.NextIndex(pointIndex, GetPointCount()));
            Vector3 positionPrev = GetPosition(SplineUtility.PreviousIndex(pointIndex, GetPointCount()));
            Vector3 forward = Vector3.forward;
            
            if (SplineEditorCache.IsValid() && SplineEditorCache.GetTarget() != null)
                forward = SplineEditorCache.GetTarget().transform.forward;

            float scale = Mathf.Min((positionNext - position).magnitude, (positionPrev - position).magnitude) * 0.33f;

            Vector3 leftTangent = (positionPrev - position).normalized * scale;
            Vector3 rightTangent = (positionNext - position).normalized * scale;

            if (GetTangentMode(pointIndex) == TangentMode.Continuous)
                SplineUtility.CalculateTangents(position, positionPrev, positionNext, forward, scale, out rightTangent, out leftTangent);

            SetLeftTangent(pointIndex, leftTangent);
            SetRightTangent(pointIndex, rightTangent);
        }

        public void MirrorTangent()
        {
            if (SpriteShapeTool.instance.isActive && SplineEditorCache.IsValid() && SplineEditorCache.GetTarget() != null)
            {
                var selection = SplineEditorCache.GetSelection();

                if (selection.Count != 1)
                    return;

                var mode = GetTangentMode(selection.single);

                if (mode == TangentMode.Linear)
                    return;

                var leftTangent = GetLeftTangent(selection.single);
                var rightTangent = GetRightTangent(selection.single);

                if (!Mathf.Approximately((leftTangent + rightTangent).sqrMagnitude, 0f))
                {
                    if (GUIUtility.hotControl == 0)
                        Undo.RegisterCompleteObjectUndo(SplineEditorCache.GetTarget(), "Mirror Tangents");
                    
                    if (SplineEditorCache.instance.rightTangentChanged)
                        SetLeftTangent(selection.single, rightTangent * -1);
                    else
                        SetRightTangent(selection.single, leftTangent * -1);

                    UpdateTangentCache();
                }
            }
        }
    }
}
