
using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    public class GraphLayerBarChart : GraphLayerBase
    {
        public GraphLayerBarChart(int stream, string name, string desc, Color color) : base(stream, name, desc, color) { }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded)
        {
            var endTime = startFrame + frameCount;
            int nextFrame = endTime;
            var stream = e.GetStream(m_stream);
            if (stream != null)
            {
                int refCountMaxDisplayVal = Mathf.Max(10, (stream.maxValue / 10 + 1) * 10);
                for (int i = stream.samples.Count - 1; i >= 0 && nextFrame > startFrame; --i)
                {
                    if (stream.samples[i].value > 0)
                    {
                        var frame = Mathf.Max(stream.samples[i].frame, startFrame);
                        var x = r.xMin + GraphUtility.ValueToPixel(frame, startFrame, endTime, r.width);
                        var w = (r.xMin + GraphUtility.ValueToPixel(nextFrame, startFrame, endTime, r.width)) - x;
                        float pixelVal = GraphUtility.ValueToPixel(stream.samples[i].value, 0, refCountMaxDisplayVal, r.height);
                        EditorGUI.DrawRect(new Rect(x, r.yMax - pixelVal, w, pixelVal), m_color);
                    }
                    nextFrame = stream.samples[i].frame;
                }
            }
        }
    }

}