using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    public class GraphLayerBackgroundGraph : GraphLayerBase
    {
        Color m_bgColor;
        int m_visibilityStream;
        public GraphLayerBackgroundGraph(int stream, int visStream, string name, string desc, Color bgColor, Color fgColor) : base(stream, name, desc, fgColor) { m_bgColor = bgColor; m_visibilityStream = visStream; }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
        {
            var endTime = startFrame + frameCount;
            int nextFrame = endTime;
            var visStream = e.GetStream(m_visibilityStream);
            var stream = e.GetStream(m_stream);
            if (stream != null && visStream != null)
            {
                for (int i = stream.samples.Count - 1; i >= 0 && nextFrame > startFrame; --i)
                {
                    var frame = Mathf.Max(stream.samples[i].frame, startFrame);
                    if (visStream.GetValue(frame) > 0)
                    {
                        var loadedVal = stream.GetValue(stream.samples[i].frame);
                        var x = r.xMin + GraphUtility.ValueToPixel(frame, startFrame, endTime, r.width);
                        var w = (r.xMin + GraphUtility.ValueToPixel(nextFrame, startFrame, endTime, r.width)) - x;
                        EditorGUI.DrawRect(new Rect(x, r.yMin, w, r.height), Color.Lerp(m_bgColor, color, (loadedVal * .01f)));
                    }
                    nextFrame = stream.samples[i].frame;
                }
            }
        }
    }
}
