using System.Collections.Generic;

namespace UnityEditor.U2D
{
    public interface ISelection : IEnumerable<int>
    {
        int Count { get; }
        int single { get; }
        int any { get; }
        void Clear();
        void BeginSelection();
        void EndSelection(bool select);
        void Select(int index, bool select);
        bool IsSelected(int index);
    }
}
