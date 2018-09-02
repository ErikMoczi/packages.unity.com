using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    internal class GraphLayerLabel : GraphLayerBase
    {
        System.Func<int, string> labelFunc;
        internal GraphLayerLabel(int stream, string name, string desc, Color color, System.Func<int, string> func) : base(stream, name, desc, color) { labelFunc = func; }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded, Material mat, int maxValue)
        {
            var endTime = startFrame + frameCount;
            var stream = e.GetStream(m_stream);
            if (stream != null)
            {
                var prevCol = GUI.color;
                GUI.color = color;
                if (expanded)
                {
                    var text = new GUIContent(maxValue.ToString());
                    var size = GUI.skin.label.CalcSize(text);
                    var rect = new Rect(r.xMin + 2, r.yMin, size.x, size.y);
                    EditorGUI.LabelField(rect, text);
                    rect = new Rect(r.xMax - size.x, r.yMin, size.x, size.y);
                    EditorGUI.LabelField(rect, text);
                }

                if (inspectFrame != endTime)
                {
                    var val = stream.GetValue(inspectFrame);
                    if (val > 0)
                    {
                        var text = new GUIContent(labelFunc(val));
                        var size = GUI.skin.label.CalcSize(text);
                        var x = GraphUtility.ValueToPixel(inspectFrame, startFrame, endTime, r.width);
                        float pixelVal = GraphUtility.ValueToPixel(val, 0, maxValue, r.height);
                        var rect = new Rect(r.xMin + x + 5, r.yMax - (pixelVal + size.y), size.x, size.y);
                        GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, .75f), 50, 5);
                        EditorGUI.LabelField(rect, text, GUI.skin.label);
                    }
                }
                GUI.color = prevCol;
            }
        }
    }
}
