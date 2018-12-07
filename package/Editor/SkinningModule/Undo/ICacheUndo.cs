using UnityEngine;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface ICacheUndo
    {
        IUndo undoOverride { get; set; }
        bool isUndoOperationSet { get; }
        void IncrementCurrentGroup();
        void BeginUndoOperation(string name);
        void EndUndoOperation();
    }
}
