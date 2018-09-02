using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;
using UnityEditor.IMGUI.Controls;

namespace EditorDiagnostics
{
    public class EventGraphListView : TreeView
    {
        class DataStreamEntry : TreeViewItem
        {
            public GUIContent m_content;
            public EventDataCollection.PlayerSession.DataSet m_entry;
            public DataStreamEntry(EventDataCollection.PlayerSession.DataSet e, int depth) : base(e.name.GetHashCode(), depth)
            {
                m_entry = e;
                displayName = e.name;
                m_content = new GUIContent(displayName);
            }
        }
        Dictionary<int, bool> maximizedState = new Dictionary<int, bool>();
        Func<string, bool> filterFunc;
        EventDataCollection.PlayerSession m_data;
        float textHeight;
        int m_inspectFrame = -1;
        public int visibleStartTime = 0;
        public int visibleDuration = 300;
        internal EventGraphListView(EventDataCollection.PlayerSession data, TreeViewState tvs, MultiColumnHeaderState mchs, Func<string, bool> filter) : base(tvs, new MultiColumnHeader(mchs))
        {
            showBorder = true;
            m_data = data;
            filterFunc = filter;
            textHeight = EditorStyles.label.CalcHeight(GUIContent.none, 1000);
            columnIndexForTreeFoldouts = 1;
        }

        protected override TreeViewItem BuildRoot()
        {
            return AddItems(new DataStreamEntry(m_data.rootStreamEntry, -1));
        }

        private DataStreamEntry AddItems(DataStreamEntry root)
        {
            root.children = new List<TreeViewItem>();
            if (!root.m_entry.hasChildren)
                return root;

            foreach (var e in root.m_entry.m_children)
            {
                if (filterFunc(e.Value.graph))
                {
                    var item = new DataStreamEntry(e.Value, root.depth + 1);
                    root.AddChild(item);
                    AddItems(item);
                }
            }
            return root;
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            return IsItemMaximized(item.id) ? 100 : base.GetCustomRowHeight(row, item);
        }

        public void OnGUI(Rect rect, int inspectFrame)
        {
            EditorGUI.DrawRect(GetGraphRect(), new Color(.15f, .15f, .15f, 1));
            m_inspectFrame = inspectFrame;
            if (Event.current.type == EventType.Repaint)
                multiColumnHeader.state.columns[2].width = rect.width - (multiColumnHeader.state.columns[1].width + multiColumnHeader.state.columns[0].width + 20);

            visibleDuration = Mathf.Max(300, (int)(multiColumnHeader.state.columns[2].width));
            if (m_data.m_isActive)
                visibleStartTime = m_data.latestFrame - visibleDuration;

            base.OnGUI(rect);
        }

        public Rect GetGraphRect()
        {
            return new Rect(treeViewRect.x + (multiColumnHeader.state.columns[1].width + multiColumnHeader.state.columns[0].width), treeViewRect.y,
                multiColumnHeader.state.columns[2].width, treeViewRect.height);
        }

        bool IsItemMaximized(int id)
        {
            bool expanded = false;
            maximizedState.TryGetValue(id, out expanded);
            return expanded;
        }

        void ToggleItemMaximize(int id)
        {
            bool expanded = IsItemMaximized(id);
            maximizedState[id] = !expanded;
            Reload();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
                CellGUI(args.GetCellRect(i), args.item as DataStreamEntry, args.GetColumn(i), ref args);
        }

        GUIContent plus = new GUIContent("+", "Expand");
        GUIContent minus = new GUIContent("-", "Collapse");

        private void CellGUI(Rect cellRect, DataStreamEntry item, int column, ref RowGUIArgs args)
        {
            switch (column)
            {
                case 0:
                {
                    var maximized = IsItemMaximized(item.id);
                    if (GUI.Button(cellRect, maximized ? minus : plus, EditorStyles.toolbarButton))
                    {
                        if (!IsSelected(item.id))
                        {
                            maximizedState[item.id] = !maximized;
                        }
                        else
                        {
                            foreach (var i in GetSelection())
                                maximizedState[i] = !maximized;
                        }
                        Reload();
                    }
                }
                break;
                case 1:
                {
                    cellRect.xMin += (GetContentIndent(item) + extraSpaceBeforeIconAndLabel);
                    EditorGUI.LabelField(cellRect, item.m_content);
                }
                break;
                case 2:
                    DrawGraph(item.m_entry, cellRect, visibleStartTime, visibleDuration, IsItemMaximized(item.id), textHeight, IsItemMaximized(item.id));
                    break;
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            ToggleItemMaximize(id);
            base.DoubleClickedItem(id);
        }

        public void DefineGraph(string name, int maxValueStream, params IGraphLayer[] layers)
        {
            graphDefs.Add(name, new GraphDefinition(maxValueStream, layers));
        }

        Material material;
        Dictionary<string, GraphDefinition> graphDefs = new Dictionary<string, GraphDefinition>();
        void DrawGraph(EventDataCollection.PlayerSession.DataSet e, Rect r, int startTime, int dur, bool drawLabels, float textHeight, bool expanded)
        {
            if (Event.current.type != EventType.Repaint)
                return;

            r = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);
            GraphDefinition gd = null;
            if (!graphDefs.TryGetValue(e.graph, out gd))
                return;

            if (material == null)
            {
				// best material options are "Unlit/Color" or "UI/Default". 
				//  Unlit/Color is more efficient, but does not support alpha
				//  UI/Default does support alpha
                material = new Material(Shader.Find("Unlit/Color"));
            }

            int maxValue = gd.GetMaxValue(e);

            foreach (var l in gd.layers)
                l.Draw(e, r, startTime, dur, m_inspectFrame, expanded, material, maxValue);
        }

        public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState()
        {
            var columns = new MultiColumnHeaderState.Column[]
            {
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column(),
                new MultiColumnHeaderState.Column()
            };

            columns[0].headerContent = new GUIContent("", "Expand");
            columns[0].minWidth = 24;
            columns[0].width = 24;
            columns[0].maxWidth = 24;
            columns[0].headerTextAlignment = TextAlignment.Center;
            columns[0].canSort = false;
            columns[0].autoResize = false;

            columns[1].headerContent = new GUIContent("Assets", "");
            columns[1].minWidth = 100;
            columns[1].width = 250;
            columns[1].maxWidth = 500;
            columns[1].headerTextAlignment = TextAlignment.Left;
            columns[1].canSort = false;
            columns[1].autoResize = false;

            columns[2].headerContent = new GUIContent("", "");
            columns[2].minWidth = 100;
            columns[2].width = 1000;
            columns[2].maxWidth = 10000;
            columns[2].headerTextAlignment = TextAlignment.Left;
            columns[2].canSort = false;
            columns[2].autoResize = false;

            return new MultiColumnHeaderState(columns);
        }
    }
}
