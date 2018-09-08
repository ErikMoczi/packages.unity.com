using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI.Treemap
{
    interface ITreemapRenderable
    {
        Color GetColor();
        Rect GetPosition();
        string GetLabel();
    }
}
