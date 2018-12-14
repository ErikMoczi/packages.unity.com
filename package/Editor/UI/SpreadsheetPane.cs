using UnityEngine;
using UnityEditor;
using Unity.MemoryProfiler.Editor.Debuging;
using System.Collections.Generic;

namespace Unity.MemoryProfiler.Editor.UI
{
    internal class SpreadsheetPane : ViewPane
    {
        public string TableDisplayName
        {
            get
            {
                return m_Spreadsheet.SourceTable.GetDisplayName();
            }
        }

        UI.DatabaseSpreadsheet m_Spreadsheet;
        Database.TableLink m_CurrentTableLink;

        public int CurrentTableIndex { get; private set; }

        bool m_NeedRefresh = false;
        
        internal class History : HistoryEvent
        {
            readonly Database.TableLink m_Table;
            readonly DatabaseSpreadsheet.State m_SpreadsheetState;

            public History(SpreadsheetPane spreadsheetPane, UIState.BaseMode mode, Database.CellLink cell)
            {
                Mode = mode;
                m_Table = spreadsheetPane.m_CurrentTableLink;
                m_SpreadsheetState = spreadsheetPane.m_Spreadsheet.CurrentState;
            }
            
            public void Restore(SpreadsheetPane pane)
            {
                DebugUtility.DebugLog("Open History: " + ToString());
                var table = pane.m_UIState.CurrentMode.GetSchema().GetTableByLink(m_Table);
                if (table == null)
                {
                    DebugUtility.LogError("No table named '" + m_Table.name + "' found.");
                    return;
                }
                pane.m_CurrentTableLink = m_Table;
                pane.CurrentTableIndex = pane.m_UIState.CurrentMode.GetTableIndex(table);
                pane.m_Spreadsheet = new UI.DatabaseSpreadsheet(pane.m_UIState.DataRenderer, table, pane, m_SpreadsheetState);
                pane.m_Spreadsheet.onClickLink += pane.OnSpreadsheetClick;
                pane.m_EventListener.OnRepaint();
            }

            public override string ToString()
            {
                string s = Mode.GetSchema().GetDisplayName() + seperator + m_Table.name;
                if (m_Table.param != null && m_Table.param.param != null)
                {
                    s += "(";
                    string sp = "";
                    foreach (var p in m_Table.param.param)
                    {
                        if (sp != "")
                        {
                            sp += ", ";
                        }
                        sp += p.Key;
                        sp += "=";
                        sp += p.Value.GetValueString(0);
                    }
                    s += sp + ")";
                }
                return s;
            }
        }

        public SpreadsheetPane(UIState s, IViewPaneEventListener l)
            : base(s, l)
        {
        }

        protected void CloseCurrentTable()
        {
            if (m_Spreadsheet != null)
            {
                if (m_Spreadsheet.SourceTable is Database.ExpandTable)
                {
                    (m_Spreadsheet.SourceTable as Database.ExpandTable).ResetAllGroup();
                }
            }
        }

        public void OpenLinkRequest(Database.View.LinkRequest link)
        {
            var tableLink = new Database.TableLink(link.metaLink.linkViewName, link.param);
            var table = m_UIState.CurrentMode.GetSchema().GetTableByLink(tableLink);
            if (table == null)
            {
                UnityEngine.Debug.LogError("No table named '" + link.metaLink.linkViewName + "' found.");
                return;
            }
            OpenLinkRequest(link, tableLink, table);
        }

        public bool OpenLinkRequest(Database.View.LinkRequest link, Database.TableLink tableLink, Database.Table table)
        {
            using (ScopeDebugContext.String("OpenLinkRequest"))
            {
                if (link.metaLink.linkWhere != null && link.metaLink.linkWhere.Count > 0)
                {
                    Database.Table filteredTable = table;
                    if (table.GetMetaData().defaultFilter != null)
                    {
                        filteredTable = table.GetMetaData().defaultFilter.CreateFilter(table);
                    }
                    var whereUnion = new Database.View.WhereUnion(link.metaLink.linkWhere, null, null, null, null, m_UIState.CurrentMode.GetSchema(), filteredTable, link.sourceView == null ? null : link.sourceView.expressionParsingContext);
                    long rowToSelect = whereUnion.GetIndexFirstMatch(link.row);
                    if (rowToSelect < 0)
                    {
                        DebugUtility.LogWarning("Could not find entry in target table '" + link.metaLink.linkViewName + "'");
                        return false;
                    }
                    DebugUtility.DebugLog("Opening table '" + link.metaLink.linkViewName + "' at row " + rowToSelect);
                    OpenTable(tableLink, table, new Database.CellPosition(rowToSelect, 0));
                }
                else
                {
                    OpenTable(tableLink, table, new Database.CellPosition(0, 0));
                }
                return true;
            }
        }

        void OnSpreadsheetClick(UI.DatabaseSpreadsheet sheet, Database.View.LinkRequest link, Database.CellPosition pos)
        {
            var hEvent = new History(this, m_UIState.CurrentMode, sheet.DisplayTable.GetLinkTo(pos));
            m_UIState.history.AddEvent(hEvent);
            m_EventListener.OnOpenTable(link);
        }

        public void OpenTable(Database.TableLink link, Database.Table table)
        {
            Profiling.StartProfiling("Profile_OpenTable_" + table.GetName());
            CloseCurrentTable();
            m_CurrentTableLink = link;
            CurrentTableIndex = m_UIState.CurrentMode.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_EventListener.OnRepaint();
        }

        public void OpenTable(Database.TableLink link, Database.Table table, Database.CellPosition pos)
        {
            CloseCurrentTable();
            m_CurrentTableLink = link;
            CurrentTableIndex = m_UIState.CurrentMode.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_Spreadsheet.Goto(pos);
            m_EventListener.OnRepaint();
        }

        public void OpenHistoryEvent(History e)
        {
            if (e == null) return;
            e.Restore(this);
        }

        public override UI.HistoryEvent GetCurrentHistoryEvent()
        {
            if (m_Spreadsheet != null && m_CurrentTableLink != null)
            {
                var c = m_Spreadsheet.GetLinkToCurrentSelection();
                if (c == null)
                {
                    c = m_Spreadsheet.GetLinkToFirstVisible();
                }
                if (c != null)
                {
                    var hEvent = new History(this, m_UIState.CurrentMode, c);
                    return hEvent;
                }
            }
            return null;
        }

        private void OnGUI_OptionBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var ff = GUILayout.Toggle(m_UIState.DataRenderer.flattenFields, "Flatten Fields");
            if (m_UIState.DataRenderer.flattenFields != ff)
            {
                m_UIState.DataRenderer.flattenFields = ff;
                if (m_Spreadsheet != null)
                {
                    m_NeedRefresh = true;
                }
            }
            var fsf = GUILayout.Toggle(m_UIState.DataRenderer.flattenStaticFields, "Flatten Static Fields");
            if (m_UIState.DataRenderer.flattenStaticFields != fsf)
            {
                m_UIState.DataRenderer.flattenStaticFields = fsf;
                if (m_Spreadsheet != null)
                {
                    m_NeedRefresh = true;
                }
            }
            var spn = GUILayout.Toggle(m_UIState.DataRenderer.ShowPrettyNames, "Pretty Name");
            if (m_UIState.DataRenderer.ShowPrettyNames != spn)
            {
                m_UIState.DataRenderer.ShowPrettyNames = spn;
                m_EventListener.OnRepaint();
            }
#if MEMPROFILER_DEBUG_INFO
            var sdv = GUILayout.Toggle(m_UIState.m_DataRenderer.showDebugValue, "Debug Value");
            if (m_UIState.m_DataRenderer.showDebugValue != sdv)
            {
                m_UIState.m_DataRenderer.showDebugValue = sdv;
                m_EventListener.OnRepaint();
            }
#endif
            EditorGUILayout.EndHorizontal();
        }

        public override void OnGUI(Rect r)
        {
            if(Event.current.type == EventType.Layout)
            {

                if (m_NeedRefresh)
                {
                    m_Spreadsheet.UpdateTable();
                    m_NeedRefresh = false;
                }
            }
            m_UIState.DataRenderer.forceLinkAllObject = false;
            if (m_Spreadsheet != null)
            {
                GUILayout.BeginArea(r);
                EditorGUILayout.BeginVertical();
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label("Filters:");

                m_Spreadsheet.OnGui_Filters();
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                m_Spreadsheet.OnGUI(r.width);
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();

                OnGUI_OptionBar();
                GUILayout.Space(2);
                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
                if (m_NeedRefresh)
                {
                    m_EventListener.OnRepaint();
                }
            }
        }

        public override void OnClose()
        {
            MemoryProfilerAnalytics.SendPendingFilterChanges();
            CloseCurrentTable();
            m_Spreadsheet = null;
        }
    }
}
