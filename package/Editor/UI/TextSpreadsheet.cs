using UnityEngine;

namespace Unity.MemoryProfiler.Editor.UI
{
    public abstract class TextSpreadsheet : SpreadsheetLogic
    {
        private EllipsisStyleMetric m_EllipsisStyleMetric_Data;
        private EllipsisStyleMetric m_EllipsisStyleMetric_Header;

        protected EllipsisStyleMetric ellipsisStyleMetric_Data
        {
            get { return m_EllipsisStyleMetric_Data ?? (m_EllipsisStyleMetric_Data = new EllipsisStyleMetric(Styles.styles.numberLabel)); }
        }
        protected EllipsisStyleMetric ellipsisStyleMetric_Header
        {
            get { return m_EllipsisStyleMetric_Header ?? (m_EllipsisStyleMetric_Header = new EllipsisStyleMetric(Styles.styles.entryEven)); }
        }

        protected const float kRowHeight = 16;

        public TextSpreadsheet(SplitterStateEx splitter, IViewEventListener listener)
            : base(splitter, listener)
        {
        }

        public TextSpreadsheet(IViewEventListener listener)
            : base(listener)
        {
        }

        protected override float GetRowHeight(long row)
        {
            return kRowHeight;
        }

        protected override void DrawRow(long row, Rect r, long index, bool selected, ref GUIPipelineState pipe)
        {
            GUILayout.Space(r.height);
            if (Event.current.type == EventType.Repaint)
            {
                var background = (index % 2 == 0 ? Styles.styles.entryEven : Styles.styles.entryOdd);
                background.Draw(r, GUIContent.none, false, false, selected, false);
            }
        }

        protected void DrawTextEllipsis(string text, Rect r, GUIStyle textStyle, EllipsisStyleMetric ellipsisStyle, bool selected)
        {
            Vector2 tSize = Styles.styles.numberLabel.CalcSize(new GUIContent(text));
            if (tSize.x > r.width)
            {
                Rect rclipped = new Rect(r.x, r.y, r.width - ellipsisStyle.pixelSize.x, r.height);
                textStyle.Draw(rclipped, text, false, false, false, false);
                Rect rEllipsis = new Rect(r.xMax - ellipsisStyle.pixelSize.x, r.y, ellipsisStyle.pixelSize.x, r.height);
                ellipsisStyle.style.Draw(rEllipsis, ellipsisStyle.ellipsisString, false, false, false, false);
            }
            else
            {
                textStyle.Draw(r, text, false, false, false, false);
            }
        }
    }


    class TestTextSpreadsheet : TextSpreadsheet
    {
        public TestTextSpreadsheet()
            : base(new SplitterStateEx(new[] { 100, 50, 50, 50 }), null)
        {
        }
        protected override long GetFirstRow()
        {
            return 0;
        }

        protected override long GetNextVisibleRow(long row)
        {
            if (row >= 1000)
            {
                return -1;
            }
            return row + 1;
        }

        protected override long GetPreviousVisibleRow(long row)
        {
            return row - 1;
        }

        protected override DirtyRowRange SetCellExpanded(long row, long col, bool expanded)
        {
            return DirtyRowRange.NonDirty;
        }

        protected override bool GetCellExpanded(long row, long col)
        {
            return false;
        }

        protected override void DrawCell(long row, long col, Rect r, long index, bool selected, ref GUIPipelineState pipe)
        {
            if (Event.current.type == EventType.Repaint)
            {
                string t = "R" + row + "C" + col + "Y" + r.y;

                DrawTextEllipsis(t, r, Styles.styles.numberLabel, ellipsisStyleMetric_Data, selected);
            }
        }

        protected override void DrawHeader(long col, Rect r, ref GUIPipelineState pipe)
        {
            Styles.styles.header.Draw(r, "Header" + col, false, false, false, false);
        }
    }
}
