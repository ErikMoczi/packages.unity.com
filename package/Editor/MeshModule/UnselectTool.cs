using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    internal class UnselectTool : IGUITool
    {
        public ISelection selection { get; set; }
        public int controlID { get { return -1; } }

        protected ScriptableObject selectionUndoableObject
        {
            get; private set;
        }

        public UnselectTool(ScriptableObject selUndo)
        {
            selectionUndoableObject = selUndo;
        }

        public void Select()
        {
            selection.Clear();
        }

        public void OnInspectorGUI() {}

        public void OnGUI()
        {
            Event ev = Event.current;

            if (selection.Count > 0 && ev.type == EventType.MouseDown && ev.button == 1 && !ev.alt)
            {
                Undo.RegisterCompleteObjectUndo(selectionUndoableObject, "Clear Selection");
                Select();
                ev.Use();
            }
        }
    }
}
