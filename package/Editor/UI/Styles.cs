using UnityEngine;
using UnityEditor;

namespace Unity.MemoryProfiler.Editor.UI
{
    internal class Styles
    {
        public GUIStyle background = "OL Box";
        public GUIStyle tooltip = "AnimationEventTooltip";
        public GUIStyle tooltipArrow = "AnimationEventTooltipArrow";
        public GUIStyle bar = "ProfilerTimelineBar";
        public GUIStyle header = "OL title";
        public GUIStyle leftPane = "ProfilerTimelineLeftPane";
        public GUIStyle rightPane = "ProfilerRightPane";
        public GUIStyle entryEven = "OL EntryBackEven";
        public GUIStyle entryOdd = "OL EntryBackOdd";
        public GUIStyle numberLabel = "OL Label";
        public GUIStyle clickableLabel;// = "OL Label";
        public GUIStyle border = new GUIStyle();
        public GUIStyle foldout = "ProfilerTimelineFoldout";
        public GUIStyle profilerGraphBackground = new GUIStyle("ProfilerScrollviewBackground");

        public Styles()
        {
            bar.normal.background = bar.hover.background = bar.active.background = EditorGUIUtility.whiteTexture;
            bar.normal.textColor = bar.hover.textColor = bar.active.textColor = Color.black;
            leftPane.padding.left = 15;
            //border.border.
            clickableLabel = new GUIStyle(numberLabel);
            clickableLabel.normal.textColor = new Color(33 / 255.0f, 150 / 255.0f, 243 / 255.0f, 1.0f);
            //clickableLabel.fontStyle = FontStyle.Bold;
            //clickableLabel.font = new Font("")
        }

        private static Styles ms_Styles;
        public static Styles styles
        {
            get { return ms_Styles ?? (ms_Styles = new Styles()); }
        }
    }
    internal class EllipsisStyleMetric
    {
        public GUIStyle style;
        public string ellipsisString = "...";
        public Vector2 pixelSize = Vector2.zero;
        public EllipsisStyleMetric(GUIStyle aStyle)
        {
            style = aStyle;
            pixelSize = aStyle.CalcSize(new GUIContent(ellipsisString));
        }
    }
}
