using UnityEngine;
using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class RectSelectionTool : IGUITool
    {
        public ISelection selection { get; set; }
        public IList<Vertex2D> vertices { get; set; }
        public Rect rect { get; private set; }
        public int controlID { get { return m_ControlID; } }

        protected ScriptableObject selectionUndoableObject
        {
            get; private set;
        }

        public RectSelectionTool(ScriptableObject selUndo)
        {
            selectionUndoableObject = selUndo;
        }

        public void OnInspectorGUI() {}

        public void OnGUI()
        {
            m_ControlID = GUIUtility.GetControlID("RectSelectionTool".GetHashCode(), FocusType.Passive);

            m_RectVertexSelector.selection = selection;
            m_RectVertexSelector.vertices = vertices;
            m_RectVertexSelector.rect = rect;

            Event ev = Event.current;
            EventType eventType = ev.GetTypeForControl(m_ControlID);

            if (GUIUtility.hotControl == 0 && HandleUtility.nearestControl == m_ControlID &&
                selection.Count > 0 && eventType == EventType.MouseDown && ev.button == 0 && !ev.alt && !ev.shift)
            {
                Undo.RegisterCompleteObjectUndo(selectionUndoableObject, "Rect Selection");
                selection.Clear();
            }

            if (GUIUtility.hotControl == m_ControlID && eventType == EventType.MouseUp && ev.button == 0)
            {
                Undo.RegisterCompleteObjectUndo(selectionUndoableObject, "Rect Selection");
                selection.EndSelection(true);
            }

            EditorGUI.BeginChangeCheck();

            rect = RectSlider.Do(m_ControlID);

            if (EditorGUI.EndChangeCheck())
            {
                selection.BeginSelection();
                m_RectVertexSelector.Select();
            }

            if (eventType == EventType.Repaint && GUIUtility.hotControl == m_ControlID)
            {
                CommonDrawingUtility.DrawRect(rect, Vector3.zero, Quaternion.identity, new Color(0f, 1f, 1f, 1f), 0.05f, 0.8f);
            }
        }

        private int m_ControlID = -1;
        private RectVertexSelector m_RectVertexSelector = new RectVertexSelector();
    }
}
