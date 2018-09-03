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
        private readonly string k_DeleteCommandName = "Delete";
        private readonly string k_SoftDeleteCommandName = "SoftDelete";

        static Color kEdgeColor = Color.cyan;
        static Color kEdgeHoveredColor = Color.yellow;
        static Color kEdgeSelectedColor = Color.yellow;
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

        public Vector2 mousePosition
        {
            get { return Handles.inverseMatrix.MultiplyPoint(Event.current.mousePosition); }
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

        public void SetupLayout()
        {
            m_CreateVertexControlID = GetControlID("CreateVertex".GetHashCode(), FocusType.Passive);
            m_CreateEdgeControlID = GetControlID("CreateEdge".GetHashCode(), FocusType.Passive);
            m_SplitEdgeControlID = GetControlID("SplitEdge".GetHashCode(), FocusType.Passive);

            if (Event.current.type == EventType.Layout)
            {
                m_HoveredVertex = -1;
                m_HoveredEdge = -1;
                m_ClosestEdgeIndex = -1;
                m_MinDistance = float.MaxValue;

                if (IsActionActive(MeshEditorAction.None))
                {
                    m_HoveredVertexControlID = -1;
                    m_HoveredEdgeControlID = -1;
                }
            }
        }

        public void CancelMode()
        {
            if (mode != SpriteMeshViewMode.Selection)
            {
                if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape ||
                    Event.current.type == EventType.MouseDown && Event.current.button == 1)
                {
                    mode = SpriteMeshViewMode.Selection;
                    Event.current.Use();
                }
            }
        }

        public void LayoutVertex(Vector2 position, int index)
        {
            int controlID = GetControlID("Vertex".GetHashCode(), FocusType.Passive);

            LayoutControl(controlID, HandleUtility.DistanceToCircle(position, kVertexRadius));

            if (Event.current.type == EventType.Layout)
            {
                if (IsControlActive(controlID))
                {
                    m_HoveredVertex = index;
                    m_HoveredVertexControlID = controlID;
                }
            }
        }

        public void LayoutEdge(Vector2 startPosition, Vector2 endPosition, int index)
        {
            int controlID = GetControlID("Edge".GetHashCode(), FocusType.Passive);

            float distance = MeshModuleUtility.DistanceToSegment(startPosition, endPosition);

            LayoutControl(controlID, distance);

            if (Event.current.type == EventType.Layout)
            {
                if (IsControlActive(controlID))
                {
                    m_HoveredEdge = index;
                    m_HoveredEdgeControlID = controlID;
                }

                distance = HandleUtility.DistancePointToLineSegment(MeshModuleUtility.ClampPositionToRect(mousePosition, frame), startPosition, endPosition);

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
                LayoutControl(m_CreateVertexControlID, 0f);

            if (IsActionActive(MeshEditorAction.CreateVertex))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.CreateVertex))
            {
                GUI.changed = true;
                Event.current.Use();

                return true;
            }

            return false;
        }

        public bool DoSelectVertex(out bool additive)
        {
            additive = false;

            if (IsActionTriggered(MeshEditorAction.SelectVertex))
            {
                additive = isActionKeyDown;
                HandleUtility.Repaint();
                return true;
            }

            return false;
        }

        public bool DoMoveVertex(out Vector2 delta)
        {
            delta = Vector2.zero;

            if (IsActionTriggered(MeshEditorAction.MoveVertex))
                m_SliderPosition = MeshModuleUtility.GUIToWorld(mousePosition);

            Vector2 newPosition;
            if (DoSlider(m_HoveredVertexControlID, m_SliderPosition, out newPosition))
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
                m_SliderPosition = MeshModuleUtility.GUIToWorld(mousePosition);

            Vector2 newPosition;
            if (DoSlider(m_HoveredEdgeControlID, m_SliderPosition, out newPosition))
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
                LayoutControl(m_CreateEdgeControlID, 0f);

            if (IsActionActive(MeshEditorAction.CreateEdge))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.CreateEdge))
            {
                GUI.changed = true;
                Event.current.Use();
                return true;
            }

            return false;
        }

        public bool DoSplitEdge()
        {
            if (CanSplitEdge())
                LayoutControl(m_SplitEdgeControlID, 0f);

            if (IsActionActive(MeshEditorAction.SplitEdge))
                ConsumeMouseMoveEvents();

            if (IsActionTriggered(MeshEditorAction.SplitEdge))
            {
                Event.current.Use();
                GUI.changed = true;
                return true;
            }

            return false;
        }

        public bool DoSelectEdge(out bool additive)
        {
            additive = false;

            if (IsActionTriggered(MeshEditorAction.SelectEdge))
            {
                additive = isActionKeyDown;
                HandleUtility.Repaint();
                return true;
            }

            return false;
        }

        public bool DoRemove()
        {
            if (IsActionTriggered(MeshEditorAction.Remove))
            {
                Event.current.Use();
                GUI.changed = true;
                return true;
            }

            return false;
        }

        public void DrawVertex(Vector2 position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            PointCapNormal(0, position, Quaternion.identity, 1f, EventType.Repaint);
        }

        public void DrawVertexHovered(Vector2 position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            PointCapHovered(0, position, Quaternion.identity, 1f, EventType.Repaint);
        }

        public void DrawVertexSelected(Vector2 position)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            PointCapSelected(0, position, Quaternion.identity, 1f, EventType.Repaint);
        }

        public void BeginDrawEdges()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            CommonDrawingUtility.BeginSolidLines();
            m_TempColor = Handles.color;
        }

        public void EndDrawEdges()
        {
            if (Event.current.type != EventType.Repaint)
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
            if (isAltDown)
                return false;

            if (action == MeshEditorAction.None)
                return IsControlActive(defaultControlID);

            if (action == MeshEditorAction.CreateVertex)
            {
                if (mode == SpriteMeshViewMode.Selection)
                    return IsControlActive(defaultControlID);

                if (mode == SpriteMeshViewMode.CreateVertex)
                    return IsControlActive(m_CreateVertexControlID);
            }

            if (action == MeshEditorAction.MoveVertex)
                return IsControlActive(m_HoveredVertexControlID);

            if (action == MeshEditorAction.CreateEdge)
                return IsControlActive(m_CreateEdgeControlID);

            if (action == MeshEditorAction.SplitEdge)
                return IsControlActive(m_SplitEdgeControlID);

            if (action == MeshEditorAction.MoveEdge)
                return IsControlActive(m_HoveredEdgeControlID);

            if (action == MeshEditorAction.SelectVertex)
                return CanSelectVertex();

            if (action == MeshEditorAction.SelectEdge)
                return CanSelectEdge();

            if (action == MeshEditorAction.Remove)
                return true;

            return false;
        }

        public bool IsActionHot(MeshEditorAction action)
        {
            if (action == MeshEditorAction.None)
                return IsControlHot(0);

            if (action == MeshEditorAction.CreateVertex)
                return IsControlHot(m_CreateVertexControlID);

            if (action == MeshEditorAction.MoveVertex)
                return IsControlHot(m_HoveredVertexControlID);

            if (action == MeshEditorAction.CreateEdge)
                return IsControlHot(m_CreateEdgeControlID);

            if (action == MeshEditorAction.SplitEdge)
                return IsControlHot(m_SplitEdgeControlID);

            if (action == MeshEditorAction.MoveEdge)
                return IsControlHot(m_HoveredEdgeControlID);

            return false;
        }

        private bool IsActionTriggered(MeshEditorAction action)
        {
            if (!IsActionActive(action))
                return false;

            if (action == MeshEditorAction.CreateVertex)
            {
                if (mode == SpriteMeshViewMode.Selection)
                    return IsMouseDown(0) && clickCount == 2;

                if (mode == SpriteMeshViewMode.CreateVertex)
                    return IsMouseDown(0);
            }

            if (action == MeshEditorAction.MoveVertex)
                return IsMouseDown(0);

            if (action == MeshEditorAction.CreateEdge)
                return IsMouseDown(0);

            if (action == MeshEditorAction.MoveEdge)
                return IsMouseDown(0);

            if (action == MeshEditorAction.SplitEdge)
                return IsMouseDown(0);

            if (action == MeshEditorAction.SelectVertex)
                return IsMouseDown(0);

            if (action == MeshEditorAction.SelectEdge)
                return IsMouseDown(0);

            if (action == MeshEditorAction.Remove)
            {
                var evt = Event.current;
                if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                    && (evt.commandName == k_SoftDeleteCommandName || evt.commandName == k_DeleteCommandName))
                {
                    if (evt.type == EventType.ExecuteCommand)
                        return true;

                    evt.Use();
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
            if (Event.current.type == EventType.MouseMove || (Event.current.type == EventType.MouseDrag && Event.current.button == 0))
                Event.current.Use();
        }

        private bool CanSelectVertex()
        {
            return IsControlActive(m_HoveredVertexControlID);
        }

        private bool CanSelectEdge()
        {
            return CanMoveEdge();
        }

        private bool CanMoveEdge()
        {
            return mode == SpriteMeshViewMode.Selection && IsControlActive(m_HoveredEdgeControlID) && !IsActionActive(MeshEditorAction.CreateEdge) && !IsActionActive(MeshEditorAction.SplitEdge);
        }

        private bool CanCreateEdge()
        {
            if (mode == SpriteMeshViewMode.Selection)
                return isShiftDown && selection.Count == 1 && selection.single != hoveredVertex;

            if (mode == SpriteMeshViewMode.CreateEdge)
                return selection.Count == 1 && selection.single != hoveredVertex;

            return false;
        }

        private bool CanSplitEdge()
        {
            if (mode == SpriteMeshViewMode.Selection)
                return isShiftDown && m_ClosestEdgeIndex != -1 && hoveredVertex == -1 && selection.Count == 0;

            if (mode == SpriteMeshViewMode.SplitEdge)
                return m_ClosestEdgeIndex != -1 && hoveredVertex == -1;

            return false;
        }

        private void DrawEdge(Vector2 startPosition, Vector2 endPosition, Color color)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            Handles.color = color;
            float width = kEdgeWidth / Handles.matrix.m00;

            CommonDrawingUtility.DrawSolidLine(width, startPosition, endPosition);
        }

        private void PointCapNormal(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            LayoutControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointNormalStyle);
        }

        private void PointCapHovered(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            LayoutControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointHoveredStyle);
        }

        private void PointCapSelected(int controlID, Vector3 position, Quaternion rotation, float size, EventType eventType)
        {
            LayoutControl(controlID, HandleUtility.DistanceToCircle(position, size * 0.5f));
            if (eventType == EventType.Repaint)
                DrawGUIStyleCap(controlID, position, rotation, size, styles.pointSelectedStyle);
        }

        private void DrawGUIStyleCap(int controlID, Vector3 position, Quaternion rotation, float size, GUIStyle guiStyle)
        {
            if (Camera.current && Vector3.Dot(position - Camera.current.transform.position, Camera.current.transform.forward) < 0f)
                return;

            Handles.BeginGUI();
            guiStyle.Draw(GetGUIStyleRect(guiStyle, position), GUIContent.none, controlID);
            Handles.EndGUI();
        }

        private Rect GetGUIStyleRect(GUIStyle style, Vector3 position)
        {
            Vector2 vector = HandleUtility.WorldToGUIPoint(position);

            float fixedWidth = style.fixedWidth;
            float fixedHeight = style.fixedHeight;

            return new Rect(vector.x - fixedWidth / 2f, vector.y - fixedHeight / 2f, fixedWidth, fixedHeight);
        }

        private int clickCount
        {
            get { return Event.current.clickCount; }
        }

        private bool isShiftDown
        {
            get { return Event.current.shift; }
        }

        private bool isAltDown
        {
            get { return Event.current.alt; }
        }

        private bool isActionKeyDown
        {
            get { return EditorGUI.actionKey; }
        }

        private bool IsMouseDown(int button)
        {
            return Event.current.type == EventType.MouseDown && Event.current.button == button;
        }

        private int GetControlID(int hint, FocusType focusType)
        {
            return GUIUtility.GetControlID(hint, focusType);
        }

        private void LayoutControl(int controlID, float distance)
        {
            if (Event.current.type == EventType.Layout)
                HandleUtility.AddControl(controlID, distance);
        }

        private bool IsControlActive(int controlID)
        {
            return HandleUtility.nearestControl == controlID && GUIUtility.hotControl == 0;
        }

        private bool IsControlHot(int controlID)
        {
            return GUIUtility.hotControl == controlID;
        }

        private bool DoSlider(int id, Vector2 position, out Vector2 newPosition)
        {
            EditorGUI.BeginChangeCheck();

            newPosition = Slider2D.Do(id, position, null);

            return EditorGUI.EndChangeCheck();
        }

        int m_CreateVertexControlID = -1;
        int m_CreateEdgeControlID = -1;
        int m_SplitEdgeControlID = -1;
        int m_HoveredEdge = -1;
        int m_HoveredEdgeControlID = -1;
        int m_HoveredVertex = -1;
        int m_HoveredVertexControlID = -1;
        int m_ClosestEdgeIndex = -1;
        Color m_TempColor;
        float m_MinDistance;
        Vector2 m_SliderPosition;
    }
}
