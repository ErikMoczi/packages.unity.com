using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using Filter = Unity.MemoryProfiler.Editor.Database.Operation.Filter;
using Unity.MemoryProfiler.Editor.Database.Operation;

namespace Unity.MemoryProfiler.Editor.UI
{
    public class DatabaseSpreadsheet : TextSpreadsheet
    {
        protected Database.Table m_TableSource;
        protected Database.Table m_TableDisplay;
        protected DataRenderer m_DataRenderer;

        public Database.Table sourceTable
        {
            get
            {
                return m_TableSource;
            }
        }
        public Database.Table displayTable
        {
            get
            {
                return m_TableDisplay;
            }
        }
        //keep the state of each column's desired filters
        //does not contain order in which the filters should be applied
        //in order of m_TableSource columns
        protected Filter.ColumnState[] m_ColumnState;

        Filter.Multi filters = new Filter.Multi();
        Filter.Sort allLevelSortFilter = new Filter.Sort();
        //filter.DefaultSort allLevelDefaultSort = new filter.DefaultSort();


        public DatabaseSpreadsheet(DataRenderer dataRenderer, Database.Table table, IViewEventListener listener, Database.Operation.Filter.Filter filter)
            : base(listener)
        {
            m_TableSource = table;
            m_TableDisplay = table;
            m_DataRenderer = dataRenderer;

            InitSplitter();
            InitFilter(filter);
        }

        public DatabaseSpreadsheet(DataRenderer dataRenderer, Database.Table table, IViewEventListener listener)
            : base(listener)
        {
            m_TableSource = table;
            m_TableDisplay = table;
            m_DataRenderer = dataRenderer;

            InitSplitter();
            InitDefaultTableFilter();
        }

        private void InitSplitter()
        {
            var meta = m_TableSource.GetMetaData();
            int colCount = meta.GetColumnCount();
            m_ColumnState = new Filter.ColumnState[colCount];
            int[] colSizes = new int[colCount];
            for (int i = 0; i != colCount; ++i)
            {
                colSizes[i] = meta.GetColumnByIndex(i).displayDefaultWidth;
                m_ColumnState[i] = new Filter.ColumnState();
            }
            m_Splitter = new SplitterStateEx(colSizes);
        }

        private void InitEmptyFilter()
        {
            filters = new Filter.Multi();
            var ds = new Database.Operation.Filter.DefaultSort();
            ds.defaultSort = allLevelSortFilter;
            filters.filters.Add(ds);
            UpdateDisplayTable();
        }

        protected void InitFilter(Database.Operation.Filter.Filter filter)
        {
            Database.Operation.Filter.FilterCloning fc = new Database.Operation.Filter.FilterCloning();

            var deffilter = filter.Clone(fc);
            if (deffilter != null)
            {
                filters = new Filter.Multi();

                filters.filters.Add(deffilter);

                allLevelSortFilter = fc.GetFirstUniqueOf<Filter.Sort>();
                if (allLevelSortFilter == null)
                {
                    allLevelSortFilter = new Filter.Sort();
                    var ds = new Database.Operation.Filter.DefaultSort();
                    ds.defaultSort = allLevelSortFilter;
                    filters.filters.Add(ds);
                }
                bool bDirty = false;
                filters.Simplify(ref bDirty);
                UpdateDisplayTable();
            }
            else
            {
                InitEmptyFilter();
            }
        }

        protected void InitDefaultTableFilter()
        {
            var meta = m_TableSource.GetMetaData();
            if (meta.defaultFilter == null)
            {
                InitEmptyFilter();
                return;
            }
            InitFilter(meta.defaultFilter);
            //database.operation.filter.FilterCloning fc = new database.operation.filter.FilterCloning();

            //var deffilter = meta.defaultFilter.Clone(fc);
            //if (deffilter != null)
            //{
            //    filters = new filter.Multi();
            //    allLevelSortFilter = (filter.Sort)fc.GetUnique(meta.defaultAllLevelSortFilter);
            //    filters.filters.Add(deffilter);

            //    if (allLevelSortFilter == null)
            //    {
            //        allLevelSortFilter = new filter.Sort();
            //        var ds = new database.operation.filter.DefaultSort();
            //        ds.defaultSort = allLevelSortFilter;
            //        filters.filters.Add(ds);
            //    }
            //    UpdateDisplayTable();
            //}
            //else
            //{
            //    InitEmptyFilter();
            //}
        }

        public Database.CellLink GetLinkToCurrentSelection()
        {
            if (m_GUIState.m_SelectedRow >= 0)
            {
                return m_TableDisplay.GetLinkTo(new Database.CellPosition(m_GUIState.m_SelectedRow, 0));
            }
            return null;
        }

        public Database.CellLink GetLinkToFirstVisible()
        {
            if (m_GUIState.m_FirstVisibleRow >= 0)
            {
                return m_TableDisplay.GetLinkTo(new Database.CellPosition(m_GUIState.m_FirstVisibleRow, 0));
            }
            return null;
        }

        public Database.Operation.Filter.Filter GetCurrentFilterCopy()
        {
            Database.Operation.Filter.FilterCloning fc = new Database.Operation.Filter.FilterCloning();
            return filters.Clone(fc);
        }

        public void Goto(Database.CellLink cl)
        {
            Goto(cl.Apply(m_TableDisplay));
        }

        protected override long GetFirstRow()
        {
            long c = m_TableDisplay.GetRowCount();
            if (c <= 0) return -1;
            return 0;
        }

        protected override long GetNextVisibleRow(long row)
        {
            row += 1;
            if (row >= m_TableDisplay.GetRowCount())
            {
                return -1;
            }
            return row;
        }

        protected override long GetPreviousVisibleRow(long row)
        {
            return row - 1;
        }

        protected override DirtyRowRange SetCellExpanded(long row, long col, bool expanded)
        {
            DirtyRowRange o;
            o.range = m_TableDisplay.ExpandCell(row, (int)col, expanded);
            o.heightOffset = kRowHeight * o.range.length;
            return o;
        }

        protected override bool GetCellExpanded(long row, long col)
        {
            return m_TableDisplay.GetCellExpandState(row, (int)col).isExpanded;
        }

        protected override void DrawHeader(long col, Rect r, ref GUIPipelineState pipe)
        {
            var colState = m_ColumnState[col];
            if (Event.current.type == EventType.Repaint)
            {
                string str = m_DataRenderer.showPrettyNames
                    ? m_TableDisplay.GetMetaData().GetColumnByIndex((int)col).displayName
                    : m_TableDisplay.GetMetaData().GetColumnByIndex((int)col).name;
                if (colState.grouped)
                {
                    str = "[" + str + "]";
                }
                string sortName = Filter.Sort.GetSortName(colState.sorted);
                str = sortName + str;

                Styles.styles.header.Draw(r, str, false, false, false, false);
                return;
            }

            var meta = m_TableSource.GetMetaData();
            var metaCol = meta.GetColumnByIndex((int)col);
            bool canGroup = false;
            if (metaCol != null)
            {
                if (metaCol.defaultGroupAlgorithm != null)
                {
                    canGroup = true;
                }
            }


            const string strOpOn = "[x] ";
            const string strOpOff = "[ ] ";
            const string strGroup = "Group";
            const string strSortAsc = "Sort Ascending";
            const string strSortDsc = "Sort Descending";
            const string strMatch = "Match...";
            List<string> opNames = new List<string>();
            List<Action<int>> opActions = new List<Action<int>>();
            if (canGroup)
            {
                if (colState.grouped)
                {
                    opNames.Add(strOpOn + strGroup);
                    opActions.Add((x) =>
                        {
                            RemoveSubGroupFilter((int)col);
                        });
                }
                else
                {
                    opNames.Add(strOpOff + strGroup);
                    opActions.Add((x) =>
                        {
                            AddSubGroupFilter((int)col);
                        });
                }
            }

            if (colState.sorted == SortOrder.Ascending)
            {
                opNames.Add(strOpOn + strSortAsc);
                opActions.Add((x) =>
                    {
                        RemoveDefaultSortFilter();
                    });
            }
            else
            {
                opNames.Add(strOpOff + strSortAsc);
                opActions.Add((x) =>
                    {
                        SetDefaultSortFilter((int)col, SortOrder.Ascending);
                    });
            }

            if (colState.sorted == SortOrder.Descending)
            {
                opNames.Add(strOpOn + strSortDsc);
                opActions.Add((x) =>
                    {
                        RemoveDefaultSortFilter();
                    });
            }
            else
            {
                opNames.Add(strOpOff + strSortDsc);
                opActions.Add((x) =>
                    {
                        SetDefaultSortFilter((int)col, SortOrder.Descending);
                    });
            }

            opNames.Add(strMatch);
            opActions.Add((x) =>
                {
                    AddMatchFilter((int)col);
                });

            int selectedOp = EditorGUI.Popup(r, -1, opNames.ToArray());
            if (selectedOp >= 0)
            {
                opActions[selectedOp](selectedOp);
            }
        }

        public void UpdateTable()
        {
            var updater = m_TableDisplay.BeginUpdate();
            if (updater != null)
            {
                long sel = updater.OldToNewRow(m_GUIState.m_SelectedRow);

                //find the row that is still the first visible or the previous one that still exist after the uptate
                long fvr = -1;
                long fvr_before = m_GUIState.m_FirstVisibleRow;
                do
                {
                    fvr = updater.OldToNewRow(fvr_before);
                    --fvr_before;
                }
                while (fvr < 0 && fvr_before >= 0);

                //if did not find any valid first visible row, use selected row
                if (fvr < 0)
                {
                    fvr = sel;
                }

                m_TableSource.EndUpdate(updater);

                if (fvr >= 0)
                {
                    long nextRow;
                    long fvrIndex = 0;
                    float fvrY = GetCumulativeHeight(GetFirstRow(), fvr, out nextRow, ref fvrIndex);
                    long lastIndex = fvrIndex;
                    float totalh = fvrY + GetCumulativeHeight(nextRow, long.MaxValue, out nextRow, ref lastIndex);


                    m_GUIState.m_ScrollPosition.y = fvrY;
                    m_GUIState.m_FirstVisibleRowY = fvrY;
                    m_GUIState.m_FirstVisibleRow = fvr;
                    m_GUIState.m_FirstVisibleRowIndex = fvrIndex;
                    m_GUIState.m_HeightBeforeFirstVisibleRow = fvrY;
                    m_DataState.m_TotalDataHeight = totalh;
                }
                else
                {
                    m_GUIState.m_ScrollPosition = Vector2.zero;
                    m_GUIState.m_FirstVisibleRowY = 0;
                    m_GUIState.m_FirstVisibleRow = GetFirstRow();
                    m_GUIState.m_FirstVisibleRowIndex = 0;
                    m_GUIState.m_HeightBeforeFirstVisibleRow = 0;
                    long nextRow;
                    long lastIndex = 0;
                    m_DataState.m_TotalDataHeight = GetCumulativeHeight(GetFirstRow(), long.MaxValue, out nextRow, ref lastIndex);
                }

                m_GUIState.m_SelectedRow = sel;
                //m_Listener.OnRepaint();
            }
            else
            {
                m_TableDisplay.EndUpdate(updater);
            }
            //UpdateDataState();
            //ResetGUIState();
        }

        public void UpdateDisplayTable()
        {
            UpdateColumnState();
            m_TableDisplay = filters.CreateFilter(m_TableSource);

            UpdateDataState();
            ResetGUIState();
        }

        protected override void DrawCell(long row, long col, Rect r, long index, bool selected, ref GUIPipelineState pipe)
        {
            var s = m_TableDisplay.GetCellExpandState(row, (int)col);

            if (s.isColumnExpandable)
            {
                int indent = s.expandDepth * 16;
                r.x += indent;
                r.width -= indent;
                if (s.isExpandable)
                {
                    Rect rToggle = new Rect(r.x, r.y, 16, r.height);
                    bool newExpanded = GUI.Toggle(rToggle, s.isExpanded, GUIContent.none, Styles.styles.foldout);
                    if (newExpanded != s.isExpanded)
                    {
                        pipe.processMouseClick = false;
                        SetCellExpandedState(row, col, newExpanded);
                    }
                }
                r.x += 16;
                r.width -= 16;
            }

            Database.View.LinkRequest link = null;
            if (onClickLink != null)
            {
                link = m_TableDisplay.GetCellLink(new Database.CellPosition(row, (int)col));
            }
            if (Event.current.type == EventType.Repaint)
            {
                var column = m_TableDisplay.GetColumnByIndex((int)col);
                if (column != null)
                {
#if MEMPROFILER_DEBUG_INFO
                    string str;
                    if (m_DataRenderer.showDebugValue)
                    {
                        //str = "\"" + column.GetRowValueString(row) + "\" " + column.GetDebugString(row);
                        str = column.GetDebugString(row);
                    }
                    else
                    {
                        str = column.GetRowValueString(row);
                    }
#else
                    var str = column.GetRowValueString(row);
#endif
                    DrawTextEllipsis(str, r,
                        link == null ? Styles.styles.numberLabel : Styles.styles.clickableLabel
                        , ellipsisStyleMetric_Data, selected);
                }
            }
            if (link != null)
            {
                if (Event.current.type == EventType.Repaint)
                {
                    EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);
                }
            }
        }

        protected override void OnGUI_CellMouseMove(Database.CellPosition pos)
        {
        }

        protected override void OnGUI_CellMouseDown(Database.CellPosition pos)
        {
            //UnityEngine.Debug.Log("MouseDown at (" + Event.current.mousePosition.x + ", " + Event.current.mousePosition.y + " row:" + row + " col:" + col);
        }

        protected override void OnGUI_CellMouseUp(Database.CellPosition pos)
        {
            if (onClickLink != null)
            {
                var link = m_TableDisplay.GetCellLink(pos);
                if (link != null && link.metaLink != null)
                {
                    onClickLink(this, link, pos);
                }
            }
        }

        public delegate void OnClickLink(DatabaseSpreadsheet sheet, Database.View.LinkRequest link, Database.CellPosition pos);
        public OnClickLink onClickLink;


        // update m_ColumnState from filters
        protected void UpdateColumnState()
        {
            long colCount = m_TableSource.GetMetaData().GetColumnCount();
            for (long i = 0; i != colCount; ++i)
            {
                m_ColumnState[i] = new Filter.ColumnState();
            }

            filters.UpdateColumnState(m_TableSource, m_ColumnState);
        }

        public bool RemoveSubSortFilter(int colIndex, bool update = true)
        {
            if (allLevelSortFilter.sortLevel.RemoveAll(x => x.GetColumnIndex(m_TableSource) == colIndex) > 0)
            {
                bool dirty = false;
                filters.Simplify(ref dirty);
                if (update)
                {
                    UpdateDisplayTable();
                }
                return true;
            }
            return false;
        }

        // return if something change
        public bool AddSubSortFilter(int colIndex, SortOrder ss, bool update = true)
        {
            Filter.Sort.Level sl = new Filter.Sort.LevelByIndex(colIndex, ss);
            int index = allLevelSortFilter.sortLevel.FindIndex(x => x.GetColumnIndex(m_TableSource) == colIndex);
            if (index >= 0)
            {
                if (allLevelSortFilter.sortLevel[index].Equals(sl)) return false;
                allLevelSortFilter.sortLevel[index] = sl;
            }
            else
            {
                allLevelSortFilter.sortLevel.Add(sl);
            }
            if (update)
            {
                UpdateDisplayTable();
            }
            return true;
        }

        // return if something change
        public bool RemoveDefaultSortFilter(bool update = true)
        {
            bool changed = allLevelSortFilter.sortLevel.Count > 0;
            allLevelSortFilter.sortLevel.Clear();
            if (changed && update)
            {
                UpdateDisplayTable();
            }
            return changed;
        }

        public bool SetDefaultSortFilter(int colIndex, SortOrder ss, bool update = true)
        {
            allLevelSortFilter.sortLevel.Clear();

            if (ss != SortOrder.None)
            {
                Filter.Sort.Level sl = new Filter.Sort.LevelByIndex(colIndex, ss);
                allLevelSortFilter.sortLevel.Add(sl);
            }
            if (update)
            {
                UpdateDisplayTable();
            }
            return true;
        }

        // return if something change
        public bool AddSubGroupFilter(int colIndex, bool update = true)
        {
            var newFilter = new Filter.GroupByColumnIndex(colIndex, SortOrder.Ascending);


            var ds = new Database.Operation.Filter.DefaultSort();
            ds.defaultSort = allLevelSortFilter;

            var gfp = GetDeepestGroupFilter(filters);
            if (gfp.child != null)
            {
                //add the new group with the default sort filter
                var newMulti = new Filter.Multi();
                newMulti.filters.Add(newFilter);
                newMulti.filters.Add(ds);
                var subf = gfp.child.subGroupFilter;
                gfp.child.subGroupFilter = newMulti;
                newFilter.subGroupFilter = subf;
            }
            else
            {
                //add it to top, already has te default sort filter there
                newFilter.subGroupFilter = ds;
                filters.filters.Insert(0, newFilter);
            }

            if (update)
            {
                UpdateDisplayTable();
            }
            return true;
        }

        // return if something change
        public bool RemoveSubGroupFilter(long colIndex, bool update = true)
        {
            FilterParenthood<Filter.Filter, Filter.Group> fpToRemove = new FilterParenthood<Filter.Filter, Filter.Group>();

            foreach (var fp in VisitAllSubGroupFilters(filters))
            {
                if (fp.child.GetColumnIndex(m_TableSource) == colIndex)
                {
                    fpToRemove = fp;
                    break;
                }
            }

            if (fpToRemove.child != null)
            {
                if (Filter.Filter.RemoveFilter(fpToRemove.parent, fpToRemove.child))
                {
                    bool dirty = false;
                    filters.Simplify(ref dirty);
                    if (update)
                    {
                        UpdateDisplayTable();
                    }
                    return true;
                }
            }

            return false;
        }

        public bool AddMatchFilter(int colIndex, bool update = true)
        {
            var newFilter = new Filter.Match(colIndex);

            filters.filters.Insert(0, newFilter);

            if (update)
            {
                UpdateDisplayTable();
            }
            return true;
        }

        protected struct FilterParenthood<PFilter, CFilter> where PFilter : Filter.Filter where CFilter : Filter.Filter
        {
            public FilterParenthood(PFilter parent, CFilter child)
            {
                this.parent = parent;
                this.child = child;
            }

            public static implicit operator FilterParenthood<Filter.Filter, Filter.Filter>(FilterParenthood<PFilter, CFilter> a)
            {
                FilterParenthood<Filter.Filter, Filter.Filter> o = new FilterParenthood<Filter.Filter, Filter.Filter>();
                o.parent = a.parent;
                o.child = a.child;
                return o;
            }
            public PFilter parent;
            public CFilter child;
        }

        protected IEnumerable<FilterParenthood<Filter.Filter, Filter.Group>> VisitAllSubGroupFilters(Filter.Filter filter)
        {
            foreach (var f in filter.SubFilters())
            {
                if (f is Filter.Group)
                {
                    Filter.Group gf = (Filter.Group)f;
                    yield return new FilterParenthood<Filter.Filter, Filter.Group>(filter, gf);
                }
                foreach (var f2 in VisitAllSubGroupFilters(f))
                {
                    yield return f2;
                }
            }
        }

        protected FilterParenthood<Filter.Filter, Filter.Group> GetFirstSubGroupFilter(Filter.Filter filter)
        {
            var e = VisitAllSubGroupFilters(filter).GetEnumerator();
            if (e.MoveNext()) return e.Current;
            return new FilterParenthood<Filter.Filter, Filter.Group>();
        }

        protected FilterParenthood<Filter.Filter, Filter.Group> GetDeepestGroupFilter(Filter.Filter filter)
        {
            foreach (var f in filter.SubFilters())
            {
                var sgf = GetDeepestGroupFilter(f);
                if (sgf.child != null) return sgf;

                if (f is Filter.Group)
                {
                    Filter.Group gf = (Filter.Group)f;
                    return new FilterParenthood<Filter.Filter, Filter.Group>(filter, gf);
                }
            }

            return new FilterParenthood<Filter.Filter, Filter.Group>();
        }

        public void OnGui_Filters()
        {
            bool dirty = false;

            EditorGUILayout.BeginVertical();
            filters.OnGui(m_TableDisplay, ref dirty);
            allLevelSortFilter.OnGui(m_TableDisplay, ref dirty);
            EditorGUILayout.EndVertical();
            if (dirty)
            {
                UpdateDisplayTable();
            }
        }
    }
}
