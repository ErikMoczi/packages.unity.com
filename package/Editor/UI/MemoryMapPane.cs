using Unity.MemoryProfiler.Editor.UI.MemoryMap;
using UnityEngine;
namespace Unity.MemoryProfiler.Editor.UI
{
    public class MemoryMapPane : ViewPane
    {
        const int kPropertyPaneWidth = 325;
        public MemoryMap.MemoryMap m_MemoryMap;

        public class SelectedMemoryRegionSpreadsheet : UI.TextSpreadsheet
        {
            public MemoryRegion[] mRegions;
            public SelectedMemoryRegionSpreadsheet(IViewEventListener listener)
                : base(new SplitterStateEx(new int[] { 100, 75, 75, 75 }), listener)
            {
            }

            public void SetRegions(MemoryRegion[] aRngs)
            {
                mRegions = aRngs;
            }

            protected override long GetFirstRow()
            {
                if (mRegions == null || mRegions.Length == 0) return -1;
                return 0;
            }

            protected override long GetNextVisibleRow(long row)
            {
                if (mRegions == null) return -1;
                long next = row + 1;
                if (next >= mRegions.Length)
                {
                    return -1;
                }
                return next;
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
                    string txt;
                    switch (col)
                    {
                        case 0:
                            txt = mRegions[row].GetDisplayName();
                            break;
                        case 1:
                            txt = string.Format("0x{0:x}", mRegions[row].GetAddressBegin());
                            break;
                        case 2:
                            txt = (mRegions[row].GetAddressEnd() - mRegions[row].GetAddressBegin()).ToString();
                            break;
                        case 3:
                            txt = mRegions[row].GetDisplayType();
                            break;
                        default:
                            txt = "";
                            break;
                    }

                    DrawTextEllipsis(txt, r, Styles.styles.numberLabel, ellipsisStyleMetric_Data, selected);
                }
            }

            protected override void DrawHeader(long col, Rect r, ref GUIPipelineState pipe)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    string txt;
                    switch (col)
                    {
                        case 0:
                            txt = "Name";
                            break;
                        case 1:
                            txt = "Begin";
                            break;
                        case 2:
                            txt = "Size";
                            break;
                        case 3:
                            txt = "Type";
                            break;
                        default:
                            txt = "";
                            break;
                    }
                    Styles.styles.header.Draw(r, txt, false, false, false, false);
                }
            }
        }

        public SelectedMemoryRegionSpreadsheet m_MemoryRegionList;
        public MemoryMapPane(UIState s, IViewPaneEventListener l)
            : base(s, l)
        {
            m_MemoryRegionList = new SelectedMemoryRegionSpreadsheet(this);
            m_MemoryMap = new MemoryMap.MemoryMap(this);
            m_MemoryMap.Setup(m_UIState.snapshotMode.snapshot);
            m_MemoryMap.OnSelectRegions = OnSelectRegions;
        }

        private MemoryRegion[] m_NewSelection;
        public void OnSelectRegions(MemoryRegion[] rngs)
        {
            m_NewSelection = rngs;
        }

        public override HistoryEvent GetCurrentHistoryEvent()
        {
            return null;
        }

        public override void OnGUI(Rect r)
        {
            float margin = 2;
            Rect rectMap = r;
            rectMap.xMax -= kPropertyPaneWidth + margin;
            m_MemoryMap.OnGUI(rectMap);

            Rect rectRegionLost = r;
            rectRegionLost.x = rectMap.xMax + margin;
            rectRegionLost.width = kPropertyPaneWidth;
            m_MemoryRegionList.OnGUI(rectRegionLost);

            if (m_NewSelection != null)
            {
                m_MemoryRegionList.SetRegions(m_NewSelection);
                m_NewSelection = null;
            }
        }

        public override void OnClose()
        {
            m_MemoryMap.CleanupMeshes();
        }
    }
}
