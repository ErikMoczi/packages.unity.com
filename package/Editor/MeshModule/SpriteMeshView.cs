using UnityEngine;
using UnityEngine.U2D;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.U2D.Interface;
using UnityEngine.U2D.Interface;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class SpriteMeshView : ISpriteMeshView
    {
        const string kDeleteCommandName = "Delete";
        const string kSoftDeleteCommandName = "SoftDelete";
        static readonly Color kEdgeColor = Color.cyan;
        static readonly Color kEdgeHoveredColor = Color.yellow;
        static readonly Color kEdgeSelectedColor = Color.yellow;
        const float kEdgeWidth = 2f;
        const float kVertexRadius = 2.5f;

        private class Styles
        {
            public readonly GUIStyle pointNormalStyle;
            public readonly GUIStyle pointHoveredStyle;
            public readonly GUIStyle pointSelectedStyle;

            public Styles()
            {
                Texture2D pointNormal = Resources.Load<Texture2D>("dotCyan");
                Texture2D pointHovered = Resources.Load<Texture2D>("dotYellow");
                Texture2D pointSelected = Resources.Load<Texture2D>("dotYellow");

                pointNormalStyle = new GUIStyle();
                pointNormalStyle.normal.background = pointNormal;
                pointNormalStyle.fixedWidth = 8f;
                pointNormalStyle.fixedHeight = 8f;

                pointHoveredStyle = new GUIStyle();
                pointHoveredStyle.normal.background = pointHovered;
                pointHoveredStyle.fixedWidth = 8f;
                pointHoveredStyle.fixedHeight = 8f;

                pointSelectedStyle = new GUIStyle();
                pointSelectedStyle.normal.background = pointSelected;
                pointSelectedStyle.fixedWidth = 8f;
                pointSelectedStyle.fixedHeight = 8f;
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

        public SpriteMeshViewMode mode { get; set; }
        public ISelection selection { get; set; }
        public int defaultControlID { get; set; }
        public Rect frame { get; set; }
        private IGUIWrapper guiWrapper { get; set; }

        public Vector2 mouseWorldPosition
        {
            get { return guiWrapper.GUIToWorld(guiWrapper.mousePosition); }
        }

        public int hoveredVertex
        {
            get { return m_HoveredVertex; }
        }

        public int hoveredEdge
        {
            get { return m_HoveredEdge; }
        }

        public int closestEdge
        {
            get { return m_ClosestEdgeIndex; }
        }

        public SpriteMeshView(IGUIWrapper gw)
        {
            guiWrapper = gw;
        }

        public void SetupLayout()
        {
            m_CreateVertexControlID = guiWrapper.GetControlID(m_CreateVertexHashCode, FocusType.Passive);
            m_CreateEdgeControlID = guiWrapper.GetControlID(m_CreateEdgeHashCode, FocusType.Passive);
            m_SplitEdgeControlID = guiWrapper.GetControlID(m_SplitEdgeHashCode, FocusType.Passive);

            if (guiWrapper.eventType == EventType.Layout)
            {
                m_HoveredVertex = -1;
                m_HoveredVertexControlID = -1;
                m_HoveredEdge = -1;
                m_HoveredEdgeControlID = -1;
                m_ClosestEdgeIndex = -1;
                m_MinDistance = float.MaxValue;

                if (guiWrapper.IsControlHot(0))
                {
                    m_MoveVertexControlID = -1;
                    m_MoveEdgeControlID = -1;
                }
            }
        }

        public void CancelMode()
        {
            if (mode != SpriteMeshViewMode.Selection)
            {
                if (guiWrapper.IsKeyDown(KeyCode.Escape) || guiWrapper.IsMouseDown(1))
                {
                    mode = SpriteMeshViewMode.Selection;
                    guiWrapper.UseCurrentEvent();
                }
            }
        }

        public void LayoutVertex(Vector2 position, int index)
        {
            int controlID = guiWrapper.GetControlID(m_VertexHashCode, FocusType.Passive);

            guiWrapper.LayoutControl(controlID, guiWrapper.DistanceToCircle(position, kVertexRadius));

            if (guiWrapper.eventType == EventType.Layout && guiWrapper.IsControlNearest(controlID))
            {
                m_HoveredVertex = index;
                m_HoveredVertexControlID = controlID;
            }
        }

        public void LayoutEdge(Vector2 startPosition, Vector2 endPosition, int index)
        {
            int controlID = guiWrapper.GetControlID(m_EdgeHashCode, FocusType.Passive);

            float distance = guiWrapper.DistanceToSegment(startPosition, endPosition);

            guiWrapper.LayoutControl(controlID, distance);

            if (guiWrapper.eventType == EventType.Layout)
            {
                if (guiWrapper.IsControlNearest(controlID))
                {
                    m_HoveredEdge = index;
                    m_HoveredEdgeControlID = controlID;
                }

                distance = HandleUtility.DistancePointToLineSegment(MeshModuleUtility.ClampPositionToRect(mouseWorldPosition, frame), startPosition, endPosition);

                if (distance < m_MinDistance)
                {
                    m_MinDistance = distance;
                    m_ClosestEdgeIndex = index;
                }
            }
        }

        public bool DoCreateVertex()
        {
            if (mode == SpriteMeshViewMode.CreateVertex && hoveredVertex == -1)
                guiWrapper.LayoutControl(m_CreateVertexControlID, 0f);

            if (mode == SpriteMeshViewMode.CreateVertex && IsActionActive(MeshEditorAction.CreateVertex))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.CreateVertex))
            {
                guiWrapper.SetGuiChanged(true);
                guiWrapper.UseCurrentEvent();

                return true;
            }

            return false;
        }

        public bool DoSelectVertex(out bool additive)
        {
            additive = false;

            if (IsActionTriggered(MeshEditorAction.SelectVertex))
            {
                additive = guiWrapper.isActionKeyDown;
                guiWrapper.Repaint();
                return true;
            }

            return false;
        }

        public bool DoMoveVertex(out Vector2 delta)
        {
            delta = Vector2.zero;

            if (IsActionTriggered(MeshEditorAction.MoveVertex))
            {
                m_MoveVertexControlID = m_HoveredVertexControlID;
                m_SliderPosition = mouseWorldPosition;
            }

            Vector2 newPosition;
            if (guiWrapper.DoSlider(m_MoveVertexControlID, m_SliderPosition, out newPosition))
            {
                delta = newPosition - m_SliderPosition;
                m_SliderPosition = newPosition;
                return true;
            }

            return false;
        }

        public bool DoMoveEdge(out Vector2 delta)
        {
            delta = Vector2.zero;

            if (IsActionTriggered(MeshEditorAction.MoveEdge))
            {
                m_MoveEdgeControlID = m_HoveredEdgeControlID;
                m_SliderPosition = mouseWorldPosition;
            }

            Vector2 newPosition;
            if (guiWrapper.DoSlider(m_MoveEdgeControlID, m_SliderPosition, out newPosition))
            {
                delta = newPosition - m_SliderPosition;
                m_SliderPosition = newPosition;
                return true;
            }

            return false;
        }

        public bool DoCreateEdge()
        {
            if (CanCreateEdge())
                guiWrapper.LayoutControl(m_CreateEdgeControlID, 0f);

            if (IsActionActive(MeshEditorAction.CreateEdge))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.CreateEdge))
            {
                guiWrapper.SetGuiChanged(true);
                guiWrapper.UseCurrentEvent();
                return true;
            }

            return false;
        }

        public bool DoSplitEdge()
        {
            if (CanSplitEdge())
                guiWrapper.LayoutControl(m_SplitEdgeControlID, 0f);

            if (IsActionActive(MeshEditorAction.SplitEdge))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.SplitEdge))
            {
                guiWrapper.UseCurrentEvent();
                guiWrapper.SetGuiChanged(true);
                return true;
            }

            return false;
        }

        public bool DoSelectEdge(out bool additive)
        {
            additive = false;

            if (IsActionTriggered(MeshEditorAction.SelectEdge))
            {
                additive = guiWrapper.isActionKeyDown;
                guiWrapper.Repaint();
                return true;
            }

            return false;
        }

        public bool DoRemove()
        {
            if (IsActionTriggered(MeshEditorAction.Remove))
            {
                guiWrapper.UseCurrentEvent();
                guiWrapper.SetGuiChanged(true);
                return true;
            }

            return false;
        }

        public void DrawVertex(Vector2 position)
        {
            CommonDrawingUtility.DrawGUIStyleCap(0, position, Quaternion.identity, 1f, styles.pointNormalStyle);
        }

        public void DrawVertexHovered(Vector2 position)
        {
            CommonDrawingUtility.DrawGUIStyleCap(0, position, Quaternion.identity, 1f, styles.pointHoveredStyle);
        }

        public void DrawVertexSelected(Vector2 position)
        {
            CommonDrawingUtility.DrawGUIStyleCap(0, position, Quaternion.identity, 1f, styles.pointSelectedStyle);
        }

        public void BeginDrawEdges()
        {
            if (guiWrapper.eventType != EventType.Repaint)
                return;

            CommonDrawingUtility.BeginSolidLines();
            m_TempColor = Handles.color;
        }

        public void EndDrawEdges()
        {
            if (guiWrapper.eventType != EventType.Repaint)
                return;

            CommonDrawingUtility.EndLines();
            Handles.color = m_TempColor;
        }

        public void DrawEdge(Vector2 startPosition, Vector2 endPosition)
        {
            DrawEdge(startPosition, endPosition, kEdgeColor);
        }

        public void DrawEdgeHovered(Vector2 startPosition, Vector2 endPosition)
        {
            DrawEdge(startPosition, endPosition, kEdgeHoveredColor);
        }

        public void DrawEdgeSelected(Vector2 startPosition, Vector2 endPosition)
        {
            DrawEdge(startPosition, endPosition, kEdgeSelectedColor);
        }

        public bool IsActionActive(MeshEditorAction action)
        {
            if (guiWrapper.isAltDown || !guiWrapper.IsControlHot(0))
                return false;

            if (action == MeshEditorAction.None)
                return guiWrapper.IsControlNearest(defaultControlID);

            if (action == MeshEditorAction.CreateVertex)
            {
                if (mode == SpriteMeshViewMode.Selection)
                    return guiWrapper.IsControlNearest(defaultControlID);

                if (mode == SpriteMeshViewMode.CreateVertex)
                    return guiWrapper.IsControlNearest(m_CreateVertexControlID);
            }

            if (action == MeshEditorAction.MoveVertex)
                return guiWrapper.IsControlNearest(m_HoveredVertexControlID);

            if (action == MeshEditorAction.CreateEdge)
                return guiWrapper.IsControlNearest(m_CreateEdgeControlID);

            if (action == MeshEditorAction.SplitEdge)
                return guiWrapper.IsControlNearest(m_SplitEdgeControlID);

            if (action == MeshEditorAction.MoveEdge)
                return guiWrapper.IsControlNearest(m_HoveredEdgeControlID);

            if (action == MeshEditorAction.SelectVertex)
                return guiWrapper.IsControlNearest(m_HoveredVertexControlID);

            if (action == MeshEditorAction.SelectEdge)
                return mode == SpriteMeshViewMode.Selection &&
                    guiWrapper.IsControlNearest(m_HoveredEdgeControlID) &&
                    !guiWrapper.IsControlNearest(m_CreateEdgeControlID) &&
                    !guiWrapper.IsControlNearest(m_SplitEdgeControlID);

            if (action == MeshEditorAction.Remove)
                return true;

            return false;
        }

        public bool IsActionHot(MeshEditorAction action)
        {
            if (action == MeshEditorAction.None)
                return guiWrapper.IsControlHot(0);

            if (action == MeshEditorAction.CreateVertex)
                return guiWrapper.IsControlHot(m_CreateVertexControlID);

            if (action == MeshEditorAction.MoveVertex)
                return guiWrapper.IsControlHot(m_HoveredVertexControlID);

            if (action == MeshEditorAction.CreateEdge)
                return guiWrapper.IsControlHot(m_CreateEdgeControlID);

            if (action == MeshEditorAction.SplitEdge)
                return guiWrapper.IsControlHot(m_SplitEdgeControlID);

            if (action == MeshEditorAction.MoveEdge)
                return guiWrapper.IsControlHot(m_HoveredEdgeControlID);

            return false;
        }

        private bool IsActionTriggered(MeshEditorAction action)
        {
            if (!IsActionActive(action))
                return false;

            if (action == MeshEditorAction.CreateVertex)
            {
                if (mode == SpriteMeshViewMode.Selection)
                    return guiWrapper.IsMouseDown(0) && guiWrapper.clickCount == 2;

                if (mode == SpriteMeshViewMode.CreateVertex)
                    return guiWrapper.IsMouseDown(0);
            }

            if (action == MeshEditorAction.MoveVertex)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.CreateEdge)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.MoveEdge)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.SplitEdge)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.SelectVertex)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.SelectEdge)
                return guiWrapper.IsMouseDown(0);

            if (action == MeshEditorAction.Remove)
            {
                if ((guiWrapper.eventType == EventType.ValidateCommand || guiWrapper.eventType == EventType.ExecuteCommand)
                    && (guiWrapper.commandName == kSoftDeleteCommandName || guiWrapper.commandName == kDeleteCommandName))
                {
                    if (guiWrapper.eventType == EventType.ExecuteCommand)
                        return true;

                    guiWrapper.UseCurrentEvent();
                }
            }

            return false;
        }

        public Vector2 WorldToScreen(Vector2 position)
        {
            return HandleUtility.WorldToGUIPoint(position);
        }

        private void ConsumeMouseMoveEvents()
        {
            if (guiWrapper.eventType == EventType.MouseMove || (guiWrapper.eventType == EventType.MouseDrag && guiWrapper.mouseButton == 0))
                guiWrapper.UseCurrentEvent();
        }

        private bool CanCreateEdge()
        {
            if (mode == SpriteMeshViewMode.Selection)
                return guiWrapper.isShiftDown && selection.Count == 1 && !selection.IsSelected(hoveredVertex);

            if (mode == SpriteMeshViewMode.CreateEdge)
                return selection.Count == 1 && !selection.IsSelected(hoveredVertex);

            return false;
        }

        private bool CanSplitEdge()
        {
            if (mode == SpriteMeshViewMode.Selection)
                return guiWrapper.isShiftDown && m_ClosestEdgeIndex != -1 && hoveredVertex == -1 && selection.Count == 0;

            if (mode == SpriteMeshViewMode.SplitEdge)
                return m_ClosestEdgeIndex != -1 && hoveredVertex == -1;

            return false;
        }

        private void DrawEdge(Vector2 startPosition, Vector2 endPosition, Color color)
        {
            if (guiWrapper.eventType != EventType.Repaint)
                return;

            Handles.color = color;
            float width = kEdgeWidth / Handles.matrix.m00;

            CommonDrawingUtility.DrawSolidLine(width, startPosition, endPosition);
        }

        readonly int m_CreateVertexHashCode = "CreateVertex".GetHashCode();
        readonly int m_CreateEdgeHashCode = "CreateEdge".GetHashCode();
        readonly int m_SplitEdgeHashCode = "SplitEdge".GetHashCode();
        readonly int m_VertexHashCode = "Vertex".GetHashCode();
        readonly int m_EdgeHashCode = "Edge".GetHashCode();

        int m_CreateVertexControlID = -1;
        int m_CreateEdgeControlID = -1;
        int m_SplitEdgeControlID = -1;
        int m_HoveredEdge = -1;
        int m_HoveredEdgeControlID = -1;
        int m_MoveEdgeControlID = -1;
        int m_HoveredVertex = -1;
        int m_HoveredVertexControlID = -1;
        int m_MoveVertexControlID = -1;
        int m_ClosestEdgeIndex = -1;
        Color m_TempColor;
        float m_MinDistance;
        Vector2 m_SliderPosition;
    }
}
