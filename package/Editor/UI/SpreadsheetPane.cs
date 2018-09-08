using UnityEngine;
using UnityEditor;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.UI
{
    public class SpreadsheetPane : ViewPane
    {
        public UI.DatabaseSpreadsheet m_Spreadsheet;
        public Database.TableLink m_CurrentTableLink;

        public int m_CurrentTableIndex = 0;

        public SpreadsheetPane(UIState s, IViewPaneEventListener l)
            : base(s, l)
        {
        }

        protected void CloseCurrentTable()
        {
            if (m_Spreadsheet != null)
            {
                if (m_Spreadsheet.sourceTable is Database.ExpandTable)
                {
                    (m_Spreadsheet.sourceTable as Database.ExpandTable).ResetAllGroup();
                }
            }
        }

        public void OpenLinkRequest(Database.View.LinkRequest link)
        {
            var tableLink = new Database.TableLink(link.metaLink.linkViewName, link.param);
            var table = m_UIState.currentMode.GetSheme().GetTableByLink(tableLink);
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
                    var whereUnion = new Database.View.WhereUnion(link.metaLink.linkWhere, null, null, null, null, m_UIState.currentMode.GetSheme(), filteredTable, link.sourceView == null ? null : link.sourceView.expressionParsingContext);
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
            var hEvent = new UI.HETable(m_UIState.currentMode, m_CurrentTableLink, sheet.GetCurrentFilterCopy(), sheet.displayTable.GetLinkTo(pos));
            m_UIState.history.AddEvent(hEvent);
            m_EventListener.OnOpenTable(link);
        }

        public void OpenTable(Database.TableLink link, Database.Table table)
        {
            CloseCurrentTable();
            m_CurrentTableLink = link;
            m_CurrentTableIndex = m_UIState.currentMode.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.m_DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_EventListener.OnRepaint();
        }

        public void OpenTable(Database.TableLink link, Database.Table table, Database.CellPosition pos)
        {
            CloseCurrentTable();
            m_CurrentTableLink = link;
            m_CurrentTableIndex = m_UIState.currentMode.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.m_DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_Spreadsheet.Goto(pos);
            m_EventListener.OnRepaint();
        }

        public void OpenHistoryEvent(UI.HETable e)
        {
            if (e == null) return;
            DebugUtility.DebugLog("Open History: " + e.ToString());
            var table = m_UIState.currentMode.GetSheme().GetTableByLink(e.table);
            if (table == null)
            {
                UnityEngine.Debug.LogError("No table named '" + e.table.name + "' found.");
                return;
            }
            m_CurrentTableLink = e.table;
            m_CurrentTableIndex = m_UIState.currentMode.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.m_DataRenderer, table, this, e.filter);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_Spreadsheet.Goto(e.cell);
            m_EventListener.OnRepaint();
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
                    var hEvent = new UI.HETable(m_UIState.currentMode, m_CurrentTableLink, m_Spreadsheet.GetCurrentFilterCopy(), c);
                    return hEvent;
                }
            }
            return null;
        }

        private void OnGUI_OptionBar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            var ff = GUILayout.Toggle(m_UIState.m_DataRenderer.flattenFields, "Flatten Fields");
            if (m_UIState.m_DataRenderer.flattenFields != ff)
            {
                m_UIState.m_DataRenderer.flattenFields = ff;
                if (m_Spreadsheet != null)
                {
                    mbNeedRefresh = true;
                }
            }
            var fsf = GUILayout.Toggle(m_UIState.m_DataRenderer.flattenStaticFields, "Flatten Static Fields");
            if (m_UIState.m_DataRenderer.flattenStaticFields != fsf)
            {
                m_UIState.m_DataRenderer.flattenStaticFields = fsf;
                if (m_Spreadsheet != null)
                {
                    mbNeedRefresh = true;
                }
            }
            var spn = GUILayout.Toggle(m_UIState.m_DataRenderer.showPrettyNames, "Pretty Name");
            if (m_UIState.m_DataRenderer.showPrettyNames != spn)
            {
                m_UIState.m_DataRenderer.showPrettyNames = spn;
                if (m_UIState.snapshotMode != null) m_UIState.snapshotMode.UpdateTableSelectionNames();
                if (m_UIState.diffMode != null) m_UIState.diffMode.UpdateTableSelectionNames();
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

        bool mbNeedRefresh = false;
        public override void OnPreGUI()
        {
            if (mbNeedRefresh)
            {
                m_Spreadsheet.UpdateTable();
                mbNeedRefresh = false;
            }
        }

        public override void OnGUI(Rect r)
        {
            m_UIState.m_DataRenderer.forceLinkAllObject = false;
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
                if (mbNeedRefresh)
                {
                    m_EventListener.OnRepaint();
                }
            }
        }

        public override void OnClose()
        {
            CloseCurrentTable();
        }
    }
}
