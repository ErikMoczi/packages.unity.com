using System.Collections.Generic;

namespace UnityEditor.Experimental.U2D.Animation
{
    public interface ISelection : IEnumerable<int>
    {
        int Count { get; }
        int single { get; }
        void Clear();
        void BeginSelection();
        void EndSelection(bool select);
        void Select(int index, bool select);
        bool IsSelected(int index);
    }
}
