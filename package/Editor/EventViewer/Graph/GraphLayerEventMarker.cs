using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    internal class GraphLayerEventMarker : GraphLayerBase
    {
        Color m_endColor;
        internal GraphLayerEventMarker(int stream, string name, string desc, Color startColor, Color endColor) : base(stream, name, desc, startColor) { m_endColor = endColor; }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
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

    internal class GraphLayerVertValueLine : GraphLayerBase
    {
        internal GraphLayerVertValueLine(int stream, string name, string desc, Color color) : base(stream, name, desc, color) {}
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
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
                    float pixelVal = GraphUtility.ValueToPixel(stream.samples[i].value, 0, maxValue, r.height);
                    EditorGUI.DrawRect(new Rect(r.xMin + GraphUtility.ValueToPixel(frame, startFrame, endTime, r.width) - 1, r.yMax - pixelVal, 2, pixelVal), m_color);
                }
            }
        }
    }

    //internal class GraphLayerVertValueLineRects : GraphLayerBase, System.IDisposable
    //{
    //    UnityEngine.Collections.NativeArray<Graphics.DrawRectArgs> rects = new UnityEngine.Collections.NativeArray<Graphics.DrawRectArgs>(1000, UnityEngine.Collections.Allocator.Persistent);
    //    public GraphLayerVertValueLineRects(int stream, string name, string desc, Color color) : base(stream, name, desc, color) { }

    //    public void Dispose()
    //    {
    //        rects.Dispose();
    //    }

    //    public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
    //    {
    //        var stream = e.GetStream(m_stream);
    //        if (stream != null)
    //        {
    //            var da = new Graphics.DrawRectArgs();
    //            da.rect.width = 2;
    //            da.color = m_color;
    //            var endTime = startFrame + frameCount;
    //            int rectCount = 0;
    //            float xMin = r.xMin - 1;
    //            float yMax = r.yMax;
    //            int rectLength = rects.Length;
    //            float oneOverRangeTimesPixelsX = (1f / (endTime - startFrame)) * r.width;
    //            float oneOverRangeTimesPixelsY = (1f / maxValue) * r.height;

    //            for (int i = stream.samples.Count - 1; i >= 0; --i)
    //            {
    //                var sample = stream.samples[i];
    //                var frame = sample.frame;
    //                var val = sample.value;
    //                if (frame < startFrame)
    //                    break;
    //                float pixelVal = val * oneOverRangeTimesPixelsY;
    //                da.rect.x = xMin + (frame - startFrame) * oneOverRangeTimesPixelsX;
    //                da.rect.y = yMax - pixelVal;
    //                da.rect.height = pixelVal;
    //                rects[rectCount++] = da;
    //                if (rectCount >= rectLength)
    //                {
    //                    Graphics.DrawRects(rects, 0, rectCount, mat);
    //                    rectCount = 0;
    //                }
    //            }
    //            if (rectCount > 0)
    //                Graphics.DrawRects(rects, 0, rectCount, mat);
    //        }
    //    }
    //}
}
