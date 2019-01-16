using Unity.MemoryProfiler.Editor.Debuging;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEditor;
using Unity.MemoryProfiler.Editor.Database.Operation;
using System;

namespace Unity.MemoryProfiler.Editor.UI
{
    internal class MemoryMapPane : ViewPane
    {
        static class Content
		{
            public static readonly  GUIContent[] TableModesList = new GUIContent[3] {
                new GUIContent("Regions list"),
                new GUIContent("Allocations list"),
                new GUIContent("Objects list")
            };

            public static readonly  GUIContent FilterLabel = new GUIContent("Display Filters");
            public static readonly  GUIContent RowSizeLabel = new GUIContent("Row Size");
            public static readonly  GUIContent ColorSchemeLabel = new GUIContent("Color scheme");
        }

        internal class History : HistoryEvent
        {
            public MemoryMapPane.TableDisplayMode m_TableDisplay;
            MemoryMap.MemoryMap.ViewState m_State;
            DatabaseSpreadsheet.State m_TableState;

            public History(MemoryMapPane pane )
            {
                Mode = pane.m_UIState.CurrentMode;

                m_TableDisplay = pane.m_CurrentTableView;
                m_TableState = pane.m_Spreadsheet.CurrentState;
                m_State = pane.m_MemoryMap.CurrentViewState;

            }

            public void Restore(MemoryMapPane pane)
            {
                pane.m_CurrentTableView = m_TableDisplay;
                pane.m_MemoryMap.CurrentViewState = m_State;

                pane.OnSelectRegions(m_State.HighlightedAddrMin, m_State.HighlightedAddrMax);
                pane.m_Spreadsheet.CurrentState = m_TableState;
                pane.m_EventListener.OnRepaint();
            }

            public override string ToString()
            {
                return Mode.GetSchema().GetDisplayName() + seperator + "Memory Map";
            }
        }        
        MemoryMap.MemoryMap m_MemoryMap;

        UI.DatabaseSpreadsheet m_Spreadsheet;

        public override VisualElement [] VisualElements
        {
            get
            {
                if (m_VisualElements == null)
                {
                    m_VisualElements = new VisualElement[]
                    {
                        new IMGUIContainer(() => OnGUI(0))
                        {
                            name = "MemoryMap",
                            style =
                            {
                                flexGrow = 6,
                            }
                        },
                        new IMGUIContainer(() => OnGUI(1))
                        {
                            name = "MemoryMapSpreadsheet",
                            style =
                            {
                                flexGrow = 2,
                            }
                        },
                        new IMGUIContainer(() => OnGUI(2))
                        {
                            name = "MemoryMapCallstack",
                            style =
                            {
                                flexGrow = 1,
                            }
                        }
                    };
                    m_VisualElementsOnGUICalls = new Action<Rect>[]
                    {
                        OnGUI,
                        OnGUISpreadsheet,
                        OnGUICallstack,
                    };
                }
                return m_VisualElements;
            }
        }
        public enum TableDisplayMode
        {
            Regions, 
            Allocations, 
            Objects,   
        }
        TableDisplayMode m_CurrentTableView = TableDisplayMode.Regions;

        TableDisplayMode CurrentTableView
        {
            get 
            {
                 return m_CurrentTableView; 
            }

            set 
            { 
                m_CurrentTableView = value;
                UnityEditor.EditorPrefs.SetInt("Unity.MemoryProfiler.Editor.UI.MemoryMapPane.TableDisplayMode", (int)m_CurrentTableView);
            }

        }
        GUIContent[] m_DisplayElementsList = null;

        struct RowSize
        {
            public readonly ulong Size;
            public readonly GUIContent Content;

            public RowSize(ulong size)
            {
                Size = size;

                if (size < 1024)
                    Content = new GUIContent(size.ToString() + " Bytes");
                else if (size < 1024*1024)
                    Content = new GUIContent((size/1024).ToString() + " KB");
                else
                    Content = new GUIContent((size/(1024*1024)).ToString() + " MB");
            }
        };

        RowSize[] m_BytesInRowList = null;
        VisualElement m_ToolbarExtension;
        IMGUIContainer m_ToolbarExtensionPane;
        UIState.BaseMode m_ToolbarExtensionMode;


        void OnModeChanged(UIState.BaseMode newMode, UIState.ViewMode newViewMode)
        {
            if (m_ToolbarExtensionMode != null)
            {
                m_ToolbarExtensionMode.ViewPaneChanged -= OnViewPaneChanged;
                m_ToolbarExtensionMode = null;
            }

            if (newMode != null)
            {
                newMode.ViewPaneChanged += OnViewPaneChanged;
                m_ToolbarExtensionMode = newMode;
            }
            
            OnViewPaneChanged(newMode.CurrentViewPane);
        }


        void OnViewPaneChanged(ViewPane newPane)
        {   
            if (m_ToolbarExtension.IndexOf(m_ToolbarExtensionPane) != -1)
            {
                m_ToolbarExtension.Remove(m_ToolbarExtensionPane);
            }

            if (newPane == this)
            {
                m_ToolbarExtension.Add(m_ToolbarExtensionPane);
            }
        }

        public MemoryMapPane(UIState s, IViewPaneEventListener l, VisualElement toolbarExtension)
            : base(s, l)
        {
            CurrentTableView = (TableDisplayMode)UnityEditor.EditorPrefs.GetInt("Unity.MemoryProfiler.Editor.UI.MemoryMapPane.TableDisplayMode", (int)TableDisplayMode.Regions);

            m_ToolbarExtension = toolbarExtension;
            m_ToolbarExtensionPane = new IMGUIContainer(new Action(OnGUIToolbarExtension));

            s.CurrentMode.ViewPaneChanged += OnViewPaneChanged;
            s.ModeChanged += OnModeChanged;

            string[] displayElements = Enum.GetNames(typeof(MemoryMap.MemoryMap.DisplayElements));
            m_DisplayElementsList = new GUIContent[ displayElements.Length ];
            for (int i=0; i<displayElements.Length; ++i)
                m_DisplayElementsList[i] = new GUIContent(displayElements[i]);
            
            ulong maxSize = 256*1024*1024; //256 128  64,32,16,8,  4,2,1,512,  256,128,64,32
            m_BytesInRowList = new RowSize[14];
            for (int i=0; i<m_BytesInRowList.Length; ++i)            
                m_BytesInRowList[i] = new RowSize(maxSize >> i);

            m_MemoryMap = new MemoryMap.MemoryMap();
            m_MemoryMap.Setup(m_UIState.snapshotMode.snapshot);
            m_MemoryMap.RegionSelected += OnSelectRegions;
        }

        public override void OnClose()
        {
            m_MemoryMap = null;
            m_Spreadsheet = null;

            if (m_ToolbarExtensionMode != null)
                m_ToolbarExtensionMode.ViewPaneChanged -= OnViewPaneChanged;
            m_ToolbarExtensionMode = null;
        }

        public void OnSelectRegions(ulong minAddr, ulong maxAddr)
        {
            if (minAddr == maxAddr)
                return;

            var lr = new Database.View.LinkRequest();
            lr.metaLink = new Database.View.MetaLink();
            
            if (CurrentTableView == TableDisplayMode.Objects)
            {
                lr.metaLink.linkViewName = ObjectAllTable.kTableName;

                lr.metaLink.linkWhere = new List<Database.View.Where.Builder>();
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("Address", Database.Operation.Operator.GreaterEqual, new Expression.MetaExpression(minAddr.ToString(), false)));
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("Address", Database.Operation.Operator.Less, new Expression.MetaExpression(maxAddr.ToString(), false)));
            }

            if (CurrentTableView == TableDisplayMode.Allocations)
            {                    
                lr.metaLink.linkViewName = "RawNativeAllocation";
                lr.metaLink.linkWhere = new List<Database.View.Where.Builder>();
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("address", Database.Operation.Operator.GreaterEqual, new Expression.MetaExpression(minAddr.ToString(), false)));
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("address", Database.Operation.Operator.Less, new Expression.MetaExpression(maxAddr.ToString(), false)));
            }

            if (CurrentTableView == TableDisplayMode.Regions)
            {
                lr.metaLink.linkViewName = "RawNativeMemoryRegion";
                lr.metaLink.linkWhere = new List<Database.View.Where.Builder>();
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("addressBase", Database.Operation.Operator.GreaterEqual, new Expression.MetaExpression(minAddr.ToString(), false)));
                lr.metaLink.linkWhere.Add(new Database.View.Where.Builder("addressBase", Database.Operation.Operator.Less, new Expression.MetaExpression(maxAddr.ToString(), false)));
            }

            lr.sourceTable = null;
            lr.sourceColumn = null;
            lr.row = -1;
            OpenLinkRequest(lr, true);

            if (m_Spreadsheet.RowCount > 0)
            {
                m_Spreadsheet.SelectedRow = 0;
            }
        }

        public override UI.HistoryEvent GetCurrentHistoryEvent()
        {
            return new History(this);
        }

        public void RestoreHistoryEvent(UI.HistoryEvent history)
        {
            (history as History).Restore(this);
        }

        void OpenLinkRequest(Database.View.LinkRequest link, bool focus)
        {
            //TODO this code is the same as the one inSpreadsheetPane, should be put together
            using (ScopeDebugContext.String("OpenLinkRequest"))
            {                
                UIElementsHelper.SetVisibility(VisualElements[2], m_UIState.snapshotMode.snapshot.nativeAllocationSites.Count > 0 && m_CurrentTableView == TableDisplayMode.Allocations);

                var tableLink = new Database.TableLink(link.metaLink.linkViewName, link.param);
                var table = m_UIState.snapshotMode.SchemaToDisplay.GetTableByLink(tableLink);
                if (table == null)
                {
                    UnityEngine.Debug.LogError("No table named '" + link.metaLink.linkViewName + "' found.");
                    return;
                }
                if (link.metaLink.linkWhere != null && link.metaLink.linkWhere.Count > 0)
                {
                    Database.Table filteredTable = table;
                    if (table.GetMetaData().defaultFilter != null)
                    {
                        filteredTable = table.GetMetaData().defaultFilter.CreateFilter(table);
                    }
                    Database.Operation.ExpressionParsingContext expressionParsingContext = null;
                    if (link.sourceView != null)
                    {
                        expressionParsingContext = link.sourceView.expressionParsingContext;
                    }
                    var whereUnion = new Database.View.WhereUnion(link.metaLink.linkWhere, null, null, null, null, m_UIState.snapshotMode.SchemaToDisplay, filteredTable, expressionParsingContext);
                    var indices = whereUnion.GetMatchingIndices(link.row);
                    var newTab = new Database.Operation.IndexedTable(table, new ArrayRange(indices));
                    OpenTable(tableLink, newTab, focus);
                }
                else
                {
                    OpenTable(tableLink, table, new Database.CellPosition(0, 0), focus);
                }
            }
        }

        void OnSpreadsheetClick(UI.DatabaseSpreadsheet sheet, Database.View.LinkRequest link, Database.CellPosition pos)
        {
            //add current event in history
            m_UIState.AddHistoryEvent(GetCurrentHistoryEvent());

            if (link.metaLink.linkViewName == ObjectTable.kTableName)
            {
                //open object link in the same pane
                OpenLinkRequest(link, true);
            }
            else
            {
                //open the link in the spreadsheet pane
                m_EventListener.OnOpenTable(link);
            }
        }
        public void OpenTable(Database.TableLink link, Database.Table table, bool focus)
        {
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_EventListener.OnRepaint();
        }

        public void OpenTable(Database.TableLink link, Database.Table table, Database.CellPosition pos, bool focus)
        {
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_Spreadsheet.Goto(pos);
            m_EventListener.OnRepaint();
        }
        public override void OnGUI(Rect r)
        {
            if (m_Spreadsheet == null)
            {
                OnSelectRegions(0,1);
            }
 
            m_MemoryMap.OnGUI(r);
        }

        
        private void SetActiveSize(object data)
        {
            m_MemoryMap.BytesInRow = m_BytesInRowList[(int)data].Size;
        }

        void OnGUISpreadsheet(Rect r)
        {
            int currentTableView = (int)CurrentTableView;
            
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);


            if (r.width > 200)
            {
                GUILayout.Space(r.width-200);
            }            

            var popupRect = GUILayoutUtility.GetRect(Content.TableModesList[currentTableView], EditorStyles.toolbarPopup);
    
            if (EditorGUI.DropdownButton(popupRect, Content.TableModesList[currentTableView], FocusType.Passive, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < Content.TableModesList.Length; i++)
                    menu.AddItem(Content.TableModesList[i], (int)currentTableView == i, (object data)=>{ CurrentTableView = (TableDisplayMode)data; m_MemoryMap.Reselect(); }, i );
                menu.DropDown(popupRect);
            }

            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (m_Spreadsheet != null)
            { 
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                m_Spreadsheet.OnGUI(r.width-4);
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);
                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndHorizontal();
        }

        void OnGUICallstack(Rect r)
        {
            if (m_CurrentTableView == TableDisplayMode.Allocations)
            {
                long row = m_Spreadsheet.SelectedRow;

                if (row < 0)
                    return;

                var col = m_Spreadsheet.DisplayTable.GetColumnByName("allocationSiteId");

                if (col == null)
                    return;

                long id = Convert.ToInt64(col.GetRowValueString(row));

                GUI.Label (r, m_UIState.snapshotMode.snapshot.nativeAllocationSites.GetReadableCallstackForId(m_UIState.snapshotMode.snapshot.nativeCallstackSymbols,id));
            }
        }

        void OnGUIToolbarExtension()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            int activeSize = 0;

            for (int i=0; i<m_BytesInRowList.Length; ++i)
            {
                if (m_BytesInRowList[i].Size == m_MemoryMap.BytesInRow)
                    activeSize = i;
            }
            var popupRect = GUILayoutUtility.GetRect(Content.RowSizeLabel, EditorStyles.toolbarPopup);
    
            if (EditorGUI.DropdownButton(popupRect, Content.RowSizeLabel, FocusType.Passive, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < m_BytesInRowList.Length; i++)
                    menu.AddItem(m_BytesInRowList[i].Content, i == activeSize, SetActiveSize, i);
                menu.DropDown(popupRect);
            }

            popupRect = GUILayoutUtility.GetRect(Content.FilterLabel, EditorStyles.toolbarPopup);
    
            if (EditorGUI.DropdownButton(popupRect, Content.FilterLabel, FocusType.Passive, EditorStyles.toolbarPopup))
            {
                GenericMenu menu = new GenericMenu();
                for (int i = 0; i < m_DisplayElementsList.Length; i++)
                {
                    MemoryMap.MemoryMap.DisplayElements element =(MemoryMap.MemoryMap.DisplayElements)(1<<i);
                    menu.AddItem(m_DisplayElementsList[i], m_MemoryMap.GetDisplayElement(element), (object data) => m_MemoryMap.ToggleDisplayElement((MemoryMap.MemoryMap.DisplayElements)data), element);
                }
                menu.DropDown(popupRect);
            }
            EditorGUILayout.EndHorizontal();
        }
    }
}
