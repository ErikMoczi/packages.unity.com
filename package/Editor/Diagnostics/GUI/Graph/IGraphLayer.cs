using UnityEngine;

namespace UnityEditor.ResourceManagement.Diagnostics
{
    public interface IGraphLayer
    {
        string Name { get; }
        string Description { get; }
        Color GraphColor { get; }
        void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue);
    }
}
