using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    public class GraphLayerEventMarker : GraphLayerBase
    {
        Color m_endColor;
        public GraphLayerEventMarker(int stream, string name, string desc, Color startColor, Color endColor) : base(stream, name, desc, startColor) { m_endColor = endColor; }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded)
        {
            var endTime = startFrame + frameCount;
            var stream = e.GetStream(m_stream);
            if (stream != null)
            {
                for (int i = stream.samples.Count - 1; i >= 0; --i)
                {
                    var frame = stream.samples[i].frame;
                    if (frame < startFrame)
                        break;
                    EditorGUI.DrawRect(new Rect(r.xMin + GraphUtility.ValueToPixel(frame, startFrame, endTime, r.width), r.yMin, 2, r.height), stream.samples[i].value == 0 ? m_endColor : color);
                }
            }
        }
    }

}