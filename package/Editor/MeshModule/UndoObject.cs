using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IUndoObject
    {
        Object undoObject { get; set; }
        void RecordObject(string name);
        void RegisterCompleteObjectUndo(string name);
        void IncrementCurrentGroup();
        void RevertAllInCurrentGroup();
    }

    internal class UndoObject : IUndoObject
    {
        private Object m_UndoObject;

        public Object undoObject
        {
            get { return m_UndoObject; }
            set { m_UndoObject = value; }
        }

        public UndoObject(Object undoObject)
        {
            m_UndoObject = undoObject;
        }

        public void RecordObject(string name)
        {
            Debug.Assert(m_UndoObject != null);

            Undo.RecordObject(m_UndoObject, name);
        }

        public void RegisterCompleteObjectUndo(string name)
        {
            Debug.Assert(m_UndoObject != null);

            Undo.RegisterCompleteObjectUndo(m_UndoObject, name);
        }

        public void IncrementCurrentGroup()
        {
            Undo.IncrementCurrentGroup();
        }

        public void RevertAllInCurrentGroup()
        {
            Undo.RevertAllInCurrentGroup();
        }
    }
}
