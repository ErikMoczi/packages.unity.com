using UnityEngine;
using UnityEditor;
namespace EditorDiagnostics
{
    public class GraphLayerLabel : GraphLayerBase
    {
        System.Func<int, string> labelFunc;
        public GraphLayerLabel(int stream, string name, string desc, Color color, System.Func<int, string> func) : base(stream, name, desc, color) { labelFunc = func; }
        public override void Draw(EventDataCollection.PlayerSession.DataSet e, Rect r, int startFrame, int frameCount, int inspectFrame, bool expanded)
        {
            var endTime = startFrame + frameCount;
            var stream = e.GetStream(m_stream);
            if (stream != null)
            {
                var prevCol = GUI.color;
                GUI.color = color;
                int refCountMaxDisplayVal = Mathf.Max(10, (stream.maxValue / 10 + 1) * 10);
                if (expanded)
                {
                    var text = new GUIContent(refCountMaxDisplayVal.ToString());
                    var size = GUI.skin.label.CalcSize(text);
                    var rect = new Rect(r.xMin + 2, r.yMin, size.x, size.y);
                    //EditorGUI.DrawRect(rect, new Color(0, 0, 0, .5f));
                    EditorGUI.LabelField(rect, text);
                    rect = new Rect(r.xMax - size.x, r.yMin, size.x, size.y);
                    //EditorGUI.DrawRect(rect, new Color(0, 0, 0, .5f));
                    EditorGUI.LabelField(rect, text);
                }

                if (inspectFrame != endTime)
                {
                    var val = stream.GetValue(inspectFrame);
                    var text = new GUIContent(labelFunc(val));
                    var size = GUI.skin.label.CalcSize(text);
                    var x = GraphUtility.ValueToPixel(inspectFrame, startFrame, endTime, r.width);
                    float pixelVal = GraphUtility.ValueToPixel(val, 0, refCountMaxDisplayVal, r.height);
                    var rect = new Rect(r.xMin + x + 5, r.yMax - (pixelVal + size.y), size.x, size.y);
                    //EditorGUI.DrawRect(rect, new Color(0, 0, 0, .5f));
                    GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture, ScaleMode.StretchToFill, true, 0, new Color(0, 0, 0, .75f), 50, 5);
                    EditorGUI.LabelField(rect, text, GUI.skin.label);
                }
                GUI.color = prevCol;
            }
        }
    }

}