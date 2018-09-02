using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;

namespace EditorDiagnostics
{
    public class EventListView : TreeView
    {
        class EventTreeViewItem : TreeViewItem
        {
            public DiagnosticEvent m_entry;
            public EventTreeViewItem(DiagnosticEvent e) : base(e.m_id.GetHashCode() + e.m_stream, 0)
            {
                m_entry = e;
            }
        }
        List<DiagnosticEvent> events;
        Action<Rect, DiagnosticEvent, int> onColumnGUI;
        Func<DiagnosticEvent, bool> onFilterEvent;

        public DiagnosticEvent selectedEvent;

        public EventListView(TreeViewState tvs, MultiColumnHeaderState mchs, Action<Rect, DiagnosticEvent, int> onColumn, Func<DiagnosticEvent, bool> filter) : base(tvs, new MultiColumnHeader(mchs))
        {
            onColumnGUI = onColumn;
            onFilterEvent = filter;
            showBorder = true;
            showAlternatingRowBackgrounds = true;
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            selectedEvent = default(DiagnosticEvent);
            if (selectedIds.Count > 0)
                selectedEvent = (FindItem(selectedIds[0], rootItem) as EventTreeViewItem).m_entry;
        }

        protected override bool CanMultiSelect(TreeViewItem item)
        {
            return false;
        }

        public void SetEvents(List<DiagnosticEvent> e)
        {
            selectedEvent = default(DiagnosticEvent);
            events = e;
            Reload();
        }

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem root = new TreeViewItem(-1, -1);
            root.children = new List<TreeViewItem>();
            if (events != null)
            {
                foreach (var e in events)
                {
                    if (onFilterEvent(e))
                        root.AddChild(new EventTreeViewItem(e));
                }
            }
            return root;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            return rows;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as EventTreeViewItem, args.GetColumn(i), ref args);
        }

        private void CellGUI(Rect cellRect, EventTreeViewItem item, int column, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
            onColumnGUI(cellRect, item.m_entry, column);
        }

        protected override bool CanBeParent(TreeViewItem item)
        {
            return false;
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(List<string> dataColumns, List<float> sizes)
        {
            var columns = new List<MultiColumnHeaderState.Column>();
            if (dataColumns.Count != sizes.Count)
                throw new System.Exception("Column name and size lists are not the same size");
            for (int i = 0; i < dataColumns.Count; i++)
                AddColumn(columns, dataColumns[i], dataColumns[i], sizes[i]);
            return new MultiColumnHeaderState(columns.ToArray());
        }

        static void AddColumn(List<MultiColumnHeaderState.Column> columns, string name, string tt, float size)
        {
            MultiColumnHeaderState.Column col = new MultiColumnHeaderState.Column();
            col.headerContent = new GUIContent(name, tt);
            col.minWidth = size * .5f;
            col.width = size;
            col.maxWidth = size * 4;
            col.headerTextAlignment = TextAlignment.Left;
            col.canSort = false;
            col.autoResize = false;
            columns.Add(col);
        }
    }
}
