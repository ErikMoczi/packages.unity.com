using UnityEngine;
using UnityEditor;

namespace Unity.MemoryProfiler.Editor.UI
{
    public interface IViewEventListener
    {
        void OnRepaint();
    }
    public abstract class SpreadsheetLogic
    {
        //used to catch infinit loop caused by implementation
        const long kMaxRow = 100000000;

        protected const float kSmallMargin = 4;
        public IViewEventListener m_Listener;

        protected SplitterStateEx m_Splitter;


        protected class DataState
        {
            //total height of all data without scroll region. used to optimize layout calls
            public double m_TotalDataHeight = 0;//using double since this value will be maintained by offseting it.
        };
        protected DataState m_DataState;

        protected class GUIState
        {
            //rect of the scroll region for displaying data. relative to current window.
            public Rect m_RectHeader;
            public Rect m_RectData;
            public bool m_HasRectData = false;
            public Vector2 m_ScrollPosition = Vector2.zero;

            public long m_FirstVisibleRow = 0;
            public float m_FirstVisibleRowY = 0;
            public long m_FirstVisibleRowIndex = 0;//sequential index assigned to all visible row. Differ from row index if there are invisible rows
            public double m_HeightBeforeFirstVisibleRow = 0;//using double since this value will be maintained by offseting it.
            public long m_SelectedRow = -1;
        };
        protected GUIState m_GUIState = new GUIState();


        public SpreadsheetLogic(SplitterStateEx splitter, IViewEventListener listener)
        {
            m_Splitter = splitter;
            m_Listener = listener;
        }

        public SpreadsheetLogic(IViewEventListener listener)
        {
            m_Listener = listener;
        }

        protected abstract float GetRowHeight(long row);

        public struct DirtyRowRange
        {
            public Range range;
            public float heightOffset;
            public static DirtyRowRange NonDirty
            {
                get
                {
                    DirtyRowRange o;
                    o.range = Range.Empty();
                    o.heightOffset = 0;
                    return o;
                }
            }
        }
        protected abstract DirtyRowRange SetCellExpanded(long row, long col, bool expanded);
        protected abstract bool GetCellExpanded(long row, long col);

        //return -1 when reach the end.
        protected abstract long GetFirstRow();
        protected abstract long GetNextVisibleRow(long row);
        protected abstract long GetPreviousVisibleRow(long row);

        protected class GUIPipelineState
        {
            public bool processMouseClick = true;
        }

        protected abstract void DrawHeader(long col, Rect r, ref GUIPipelineState pipe);
        protected abstract void DrawRow(long row, Rect r, long index, bool selected, ref GUIPipelineState pipe);
        protected abstract void DrawCell(long row, long col, Rect r, long index, bool abSelected, ref GUIPipelineState pipe);

        protected float GetCumulativeHeight(long rowMin, long rowMaxExclusive, out long outNextRow, ref long rowMinIndex)
        {
            float h = 0;
            long i = rowMin;
            for (; i >= 0 && i < rowMaxExclusive && i >= 0; i = GetNextVisibleRow(i))
            {
                h += GetRowHeight(i);
                ++rowMinIndex;
            }
            outNextRow = i;
            return h;
        }

        protected void ResetGUIState()
        {
            m_GUIState = new GUIState();
        }

        protected void UpdateDataState()
        {
            m_DataState = new DataState();
            long iRowCount = 0;
            float h = 0;

            long i = 0;
            for (; i >= 0 && iRowCount < kMaxRow; i = GetNextVisibleRow(i), ++iRowCount)
            {
                h += GetRowHeight(i);
            }
            UnityEngine.Debug.Assert(iRowCount < kMaxRow, "GridSheet.UpdateDataState Reached " + kMaxRow + " rows while computing data state, make sure GetNextTopLevelRow() eventually returns -1");


            m_DataState.m_TotalDataHeight = h;
        }

        public void UpdateDirtyRowRange(DirtyRowRange d)
        {
            m_DataState.m_TotalDataHeight += d.heightOffset;
        }

        public void SetCellExpandedState(long row, long col, bool expanded)
        {
            DirtyRowRange dirtyRange = SetCellExpanded(row, col, expanded);
            UpdateDirtyRowRange(dirtyRange);
        }

        public void Goto(Database.CellPosition pos)
        {
            long rowIndex = 0;
            long nextRow = 0;
            var y = GetCumulativeHeight(0, pos.row, out nextRow, ref rowIndex);
            m_GUIState.m_ScrollPosition = new Vector2(0, y);
            m_GUIState.m_SelectedRow = pos.row;
            m_GUIState.m_FirstVisibleRow = pos.row;
            m_GUIState.m_FirstVisibleRowIndex = rowIndex;
            m_GUIState.m_FirstVisibleRowY = y;
            m_GUIState.m_HeightBeforeFirstVisibleRow = y;
        }

        protected virtual void OnGUI_CellMouseMove(Database.CellPosition pos)
        {
        }

        protected virtual void OnGUI_CellMouseDown(Database.CellPosition pos)
        {
        }

        protected virtual void OnGUI_CellMouseUp(Database.CellPosition pos)
        {
        }

        protected long GetColAtPosition(Vector2 pos)
        {
            var xLocal = pos.x - m_Splitter.m_TopLeft.x;
            long col = 0;
            while (col < m_Splitter.realSizes.Length)
            {
                xLocal -= m_Splitter.realSizes[col];
                if (xLocal < 0) return col;
                ++col;
            }
            return -1;
        }

        protected long GetRowAtPosition(Vector2 pos)
        {
            if (!m_GUIState.m_HasRectData) return -1;
            if (pos.x < m_GUIState.m_RectData.x || pos.y < m_GUIState.m_RectData.y) return -1;
            if (pos.x >= m_GUIState.m_RectData.xMax || pos.y >= m_GUIState.m_RectData.yMax) return -1;
            var vInData = pos - m_GUIState.m_RectData.position;
            //var yWorld = m_GUIState.m_ScrollPosition + vInData.y;
            var y = m_GUIState.m_ScrollPosition.y + vInData.y - m_GUIState.m_FirstVisibleRowY;

            float h = 0;
            long i = m_GUIState.m_FirstVisibleRowIndex;
            int iMax = 0;
            while (i >= 0 && iMax < kMaxRow)
            {
                h += GetRowHeight(i);
                if (h >= y) break;
                i = GetNextVisibleRow(i);
                ++iMax;
            }
            return i;
        }

        public void OnGUI(Rect r)
        {
            GUI.BeginGroup(r);
            OnGUI(float.PositiveInfinity);
            GUI.EndGroup();
        }

        public void OnGUI(float maxWidth, params GUILayoutOption[] opt)
        {
            GUIPipelineState pipe = new GUIPipelineState();

            if (m_DataState == null)
            {
                UpdateDataState();
            }

            GUILayout.BeginVertical(opt);

            m_Splitter.BeginHorizontalSplit(-m_GUIState.m_ScrollPosition.x);
            Rect rectHeader = new Rect(m_Splitter.m_TopLeft.x, m_Splitter.m_TopLeft.y, 0, Styles.styles.header.fixedHeight);

            float totalHeaderWidth = 0;
            for (int k = 0; k < m_Splitter.realSizes.Length; ++k)
            {
                rectHeader.width = m_Splitter.realSizes[k];
                totalHeaderWidth += rectHeader.width;

                DrawHeader(k, rectHeader, ref pipe);

                rectHeader.x += rectHeader.width;
            }
            GUILayout.Space(rectHeader.width < maxWidth ? rectHeader.width : maxWidth);

            m_Splitter.EndHorizontalSplit();


            GUILayout.Space(rectHeader.height + 1);

            var rectHeader2 = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                m_GUIState.m_RectHeader = rectHeader2;
            }

            //Rect rr = GUILayoutUtility.GetRect(10, 10000, 10, 10000, Styles.styles.background);
            //Debug.Log("rr = " + rr.xMin + ", " + rr.yMin + " - " + rr.xMax + ", " + rr.yMax);

            Vector2 scrollBefore = m_GUIState.m_ScrollPosition;
            m_GUIState.m_ScrollPosition = GUILayout.BeginScrollView(scrollBefore, Styles.styles.background);

            EditorGUILayout.BeginHorizontal();
            for (int k = 0; k < m_Splitter.realSizes.Length; ++k)
            {
                GUILayout.Space(m_Splitter.realSizes[k]);
            }
            EditorGUILayout.EndHorizontal();

            if (scrollBefore.y < m_GUIState.m_ScrollPosition.y)
            {
                //moved down
                double curYMin = m_GUIState.m_FirstVisibleRowY;
                double rowH = 0;
                double curYMax = m_GUIState.m_FirstVisibleRowY;
                double offsetY = 0;
                long curRow = 0;
                long nextRow = m_GUIState.m_FirstVisibleRow;
                long i = 0;
                do
                {
                    curRow = nextRow;
                    curYMin = curYMax;
                    offsetY += rowH;

                    rowH = GetRowHeight(curRow);
                    curYMax = curYMin + rowH;

                    nextRow = GetNextVisibleRow(nextRow);
                    ++i;
                }
                while (curYMax < m_GUIState.m_ScrollPosition.y && nextRow >= 0 && i < kMaxRow);

                UnityEngine.Debug.Assert(i < kMaxRow, "Reached " + kMaxRow + " iteration while updating data state. make sure GetRowHeight does not always return 0 or GetNextVisibleRow eventually returns -1");

                m_GUIState.m_FirstVisibleRow = curRow;
                m_GUIState.m_FirstVisibleRowY = (float)curYMin;
                m_GUIState.m_FirstVisibleRowIndex += i - 1;
                m_GUIState.m_HeightBeforeFirstVisibleRow += offsetY;
            }
            else if (scrollBefore.y > m_GUIState.m_ScrollPosition.y)
            {
                //moved up
                float curYMin = m_GUIState.m_FirstVisibleRowY;
                float rowH = 0;
                long curRow = m_GUIState.m_FirstVisibleRow;
                long i = 0;
                double offsetY = 0;
                while (curYMin > m_GUIState.m_ScrollPosition.y && curRow >= 0 && i < kMaxRow)
                {
                    var prevRow = GetPreviousVisibleRow(curRow);
                    if (prevRow < 0)
                    {
                        //can't move up any further. set all data to top of the list.
                        curYMin = 0;
                        offsetY = m_GUIState.m_HeightBeforeFirstVisibleRow;
                        break;
                    }
                    curRow = prevRow;
                    rowH = GetRowHeight(curRow);
                    offsetY += rowH;
                    curYMin = curYMin - rowH;
                    ++i;
                }
                UnityEngine.Debug.Assert(i < kMaxRow, "Reached " + kMaxRow + " iteration while updating data state. make sure GetRowHeight does not always return 0 or GetNextVisibleRow eventually returns -1");

                m_GUIState.m_FirstVisibleRow = curRow;
                m_GUIState.m_FirstVisibleRowY = curYMin;
                m_GUIState.m_FirstVisibleRowIndex -= i;
                m_GUIState.m_HeightBeforeFirstVisibleRow -= offsetY;
            }


            GUILayout.Space((float)m_GUIState.m_HeightBeforeFirstVisibleRow);

            double visibleRowTotalHeight = 0;

            float yMax = m_GUIState.m_ScrollPosition.y + m_GUIState.m_RectData.height;
            Rect r = new Rect(0
                    , m_GUIState.m_FirstVisibleRowY
                    , 0
                    , 0);
            long firstRow = GetFirstRow();
            if (firstRow >= 0)
            {
                firstRow = m_GUIState.m_FirstVisibleRow;
            }
            for (long i = firstRow, j = 0; r.y < yMax && i < kMaxRow && i >= 0; i = GetNextVisibleRow(i), ++j)
            {
                float h = GetRowHeight(i);
                r.height = h;
                visibleRowTotalHeight += h;
                r.x = 0;
                r.width = m_GUIState.m_RectData.width;
                Rect rRow = new Rect(m_GUIState.m_ScrollPosition.x, r.y, r.width, r.height);

                DrawRow(i, rRow, m_GUIState.m_FirstVisibleRowIndex + j, i == m_GUIState.m_SelectedRow, ref pipe);
                for (long k = 0; k < m_Splitter.realSizes.Length; ++k)
                {
                    if (k == 0)
                    {
                        r.xMax = m_Splitter.realSizes[k];
                    }
                    else
                    {
                        r.width = m_Splitter.realSizes[k] - kSmallMargin;
                    }
                    if (m_Splitter.realSizes[k] > 0)
                    {
                        DrawCell(i, k, r, m_GUIState.m_FirstVisibleRowIndex + j, i == m_GUIState.m_SelectedRow, ref pipe);
                    }

                    r.x += m_Splitter.realSizes[k];
                }

                r.y += h;
            }

            double heightAfterVisibleRow = m_DataState.m_TotalDataHeight - (m_GUIState.m_HeightBeforeFirstVisibleRow + visibleRowTotalHeight);

            GUILayout.Space((float)heightAfterVisibleRow);

            GUILayout.EndScrollView();

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.Repaint)
            {
                m_GUIState.m_RectData = rect;
                if (m_GUIState.m_HasRectData == false)
                {
                    m_GUIState.m_HasRectData = true;
                    if (m_Listener != null) m_Listener.OnRepaint();
                }
            }


            GUILayout.EndVertical();


            if (pipe.processMouseClick)
            {
                switch (Event.current.type)
                {
                    case EventType.MouseDown:
                    {
                        var row = GetRowAtPosition(Event.current.mousePosition);
                        if (row >= 0)
                        {
                            m_GUIState.m_SelectedRow = row;
                            if (m_Listener != null) m_Listener.OnRepaint();
                            OnGUI_CellMouseDown(new Database.CellPosition(row, (int)GetColAtPosition(Event.current.mousePosition)));
                        }
                        break;
                    }
                    case EventType.MouseUp:
                    {
                        var row = GetRowAtPosition(Event.current.mousePosition);
                        if (row >= 0)
                        {
                            var col = (int)GetColAtPosition(Event.current.mousePosition);
                            if (col >= 0)
                            {
                                OnGUI_CellMouseUp(new Database.CellPosition(row, col));
                            }
                        }
                        break;
                    }
                    case EventType.MouseMove:
                    {
                        var row = GetRowAtPosition(Event.current.mousePosition);
                        if (row >= 0)
                        {
                            var col = (int)GetColAtPosition(Event.current.mousePosition);
                            if (col >= 0)
                            {
                                OnGUI_CellMouseUp(new Database.CellPosition(row, col));
                            }
                        }
                        break;
                    }
                }
            }
        }
    }
}
