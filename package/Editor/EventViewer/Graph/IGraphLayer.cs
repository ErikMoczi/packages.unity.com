using UnityEngine;

namespace EditorDiagnostics
{
    public interface IGraphLayer
    {
        string name { get; }
        string description { get; }
        Color color { get; }
        void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue);
    }
}
