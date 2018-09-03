using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface IUndoObject
    {
        void RegisterCompleteObjectUndo(string name);
        void IncrementCurrentGroup();
    }

    internal class UndoObject : IUndoObject
    {
        public UndoObject(ScriptableObject undoObject)
        {
            m_UndoObject = undoObject;
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

        private ScriptableObject m_UndoObject;
    }
}
