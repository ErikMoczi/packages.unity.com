using UnityEngine;
namespace UnityEditor.ResourceManagement.Diagnostics
{
    internal class GraphLayerBackgroundGraph : GraphLayerBase
    {
        Color m_bgColor;
        int m_visibilityStream;
        internal GraphLayerBackgroundGraph(int stream, int visStream, string name, string desc, Color bgColor, Color fgColor) : base(stream, name, desc, fgColor) { m_bgColor = bgColor; m_visibilityStream = visStream; }
        public override void Draw(EventDataSet dataSet, Rect rect, int startFrame, int frameCount, int inspectFrame, bool expanded, Material material, int maxValue)
        {
            if (dataSet == null)
                return;

            var endTime = startFrame + frameCount;
            int nextFrame = endTime;
            var visStream = dataSet.GetStream(m_visibilityStream);
            var stream = dataSet.GetStream(Stream);
            if (stream != null && visStream != null)
            {
                for (int i = stream.m_samples.Count - 1; i >= 0 && nextFrame > startFrame; --i)
                {
                    var frame = Mathf.Max(stream.m_samples[i].frame, startFrame);
                    if (visStream.GetValue(frame) > 0)
                    {
                        var loadedVal = stream.GetValue(stream.m_samples[i].frame);
                        var x = rect.xMin + GraphUtility.ValueToPixel(frame, startFrame, endTime, rect.width);
                        var w = (rect.xMin + GraphUtility.ValueToPixel(nextFrame, startFrame, endTime, rect.width)) - x;
                        EditorGUI.DrawRect(new Rect(x, rect.yMin, w, rect.height), Color.Lerp(m_bgColor, GraphColor, (loadedVal * .01f)));
                    }
                    nextFrame = stream.m_samples[i].frame;
                }
            }
        }
    }
}
