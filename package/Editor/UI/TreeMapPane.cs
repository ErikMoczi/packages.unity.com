using UnityEngine;
using UnityEditor;
using Unity.MemoryProfiler.Editor.Debuging;

namespace Unity.MemoryProfiler.Editor.UI
{
    public class TreeMapPane : ViewPane
    {
        const int kPropertyPaneWidth = 0;
        const int kDetailsPaneHeight = 200;
        public UI.Treemap.TreeMapView m_TreeMap;

        public UI.DatabaseSpreadsheet m_Spreadsheet;
        public Database.TableLink m_CurrentTableLink;

        public TreeMapPane(UIState s, IViewPaneEventListener l)
            : base(s, l)
        {
            m_TreeMap = new UI.Treemap.TreeMapView(s.snapshotMode.m_RawScheme.m_Snapshot);
            m_TreeMap.Setup();
            m_TreeMap.OnClickItem = OnClickItem;
            m_TreeMap.OnClickGroup = OnClickGroup;
            m_TreeMap.OnOpenItem = OnOpenItem;

            ShowAllObjects(null, false);
        }

        public void ShowAllObjects(Treemap.IMetricValue itemCopyToSelect, bool focus)
        {
            Treemap.IMetricValue itemToSelect = null;
            m_TreeMap.ClearMetric();
            foreach (var managedObject in m_UIState.snapshotMode.snapshot.m_CrawledData.managedObjects)
            {
                if (managedObject.size > 0)
                {
                    var o = new Treemap.ManagedObjectMetric(m_UIState.snapshotMode.snapshot, managedObject);
                    if (o.IsSame(itemCopyToSelect))
                    {
                        itemToSelect = o;
                    }
                    m_TreeMap.AddMetric(o);
                }
            }
            for (int i = 0; i != m_UIState.snapshotMode.snapshot.nativeObjects.Count; ++i)
            {
                if (m_UIState.snapshotMode.snapshot.nativeObjects.size[i] > 0)
                {
                    var o = new Treemap.NativeObjectMetric(m_UIState.snapshotMode.snapshot, i);
                    if (o.IsSame(itemCopyToSelect))
                    {
                        itemToSelect = o;
                    }
                    m_TreeMap.AddMetric(o);
                }
            }
            m_TreeMap.UpdateMetric();

            if (itemToSelect != null)
                OpenMetricData(itemToSelect, focus);
        }

        public override UI.HistoryEvent GetCurrentHistoryEvent()
        {
            if (m_TreeMap.selectedItem == null) return null;
            var e = new UI.HETreeMap(m_UIState.currentMode, m_TreeMap.selectedItem._metric);
            return e;
        }

        public void OnClickItem(Treemap.Item a)
        {
            m_UIState.AddHistoryEvent(GetCurrentHistoryEvent());
            m_TreeMap.SelectItem(a);
            OpenMetricData(a._metric, false);
        }

        public void OnOpenItem(Treemap.Item a)
        {
            //m_EventListener.OnOpenTable;
        }

        public void OnClickGroup(Treemap.Group a)
        {
            m_UIState.AddHistoryEvent(GetCurrentHistoryEvent());
            m_TreeMap.SelectGroup(a);
        }

        void OpenMetricData(Treemap.IMetricValue metric, bool focus)
        {
            if (metric is Treemap.ManagedObjectMetric)
            {
                var m = (Treemap.ManagedObjectMetric)metric;

                var lr = new Database.View.LinkRequest();
                lr.metaLink = new Database.View.MetaLink();
                lr.metaLink.linkViewName = ObjectTable.kTableName;
                lr.sourceTable = null;
                lr.sourceColumn = null;
                lr.row = -1;
                lr.param = new Database.ParameterSet();
                lr.param.param.Add(ObjectTable.kObjParamName, new Database.Operation.ExpConst<ulong>(m.m_Object.ptrObject));
                lr.param.param.Add(ObjectTable.kTypeParamName, new Database.Operation.ExpConst<int>(m.m_Object.iTypeDescription));
                OpenLinkRequest(lr, focus);
            }
            else if (metric is Treemap.NativeObjectMetric)
            {
                var m = (Treemap.NativeObjectMetric)metric;
                var lr = new Database.View.LinkRequest();
                lr.metaLink = new Database.View.MetaLink();
                lr.metaLink.linkViewName = ObjectAllNativeTable.kTableName;

                var instanceId = m_UIState.snapshotMode.snapshot.nativeObjects.instanceId[m.m_ObjectIndex];
                var b = new Database.View.Where.Builder("NativeInstanceId", Database.Operation.Operator.equal, new Database.Operation.Expression.MetaExpression(instanceId.ToString()));
                lr.metaLink.linkWhere = new System.Collections.Generic.List<Database.View.Where.Builder>();
                lr.metaLink.linkWhere.Add(b);
                lr.sourceTable = null;
                lr.sourceColumn = null;
                lr.row = -1;
                OpenLinkRequest(lr, focus);
            }
        }

        void OpenLinkRequest(Database.View.LinkRequest link, bool focus)
        {
            //TODO this code is the same as the one inSpreadsheetPane, should be put together
            using (ScopeDebugContext.String("OpenLinkRequest"))
            {
                var tableLink = new Database.TableLink(link.metaLink.linkViewName, link.param);
                var table = m_UIState.snapshotMode.m_SchemeToDisplay.GetTableByLink(tableLink);
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
                    var whereUnion = new Database.View.WhereUnion(link.metaLink.linkWhere, null, null, null, null, m_UIState.snapshotMode.m_SchemeToDisplay, filteredTable, expressionParsingContext);
                    long rowToSelect = whereUnion.GetIndexFirstMatch(link.row);
                    if (rowToSelect < 0)
                    {
                        UnityEngine.Debug.LogError("Could not find entry in target table '" + link.metaLink.linkViewName + "'");
                        return;
                    }
                    OpenTable(tableLink, table, new Database.CellPosition(rowToSelect, 0), focus);
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

        private void SelectObjectByUID(int objectUID, bool focus)
        {
            var i = m_TreeMap.GetItemByObjectUID(objectUID);
            if (i != null)
            {
                if (focus)
                {
                    m_TreeMap.FocusOnItem(i, true);
                }
                else
                {
                    m_TreeMap.SelectItem(i);
                }
            }
        }

        private int GetTableObjectUID(Database.Table table, long row)
        {
            var indexColBase = table.GetColumnByName("Index");
            var indexColSub = indexColBase;
            while (indexColSub != null && indexColSub is Database.IColumnDecorator)
            {
                indexColSub = (indexColSub as Database.IColumnDecorator).GetBaseColumn();
            }
            if (indexColSub != null && indexColSub is ObjectListUnifiedIndexColumn)
            {
                var indexCol = (Database.ColumnTyped<int>)indexColBase;
                var objectUID = indexCol.GetRowValue(row);
                return objectUID;
            }
            return -1;
        }

        public void OpenTable(Database.TableLink link, Database.Table table, bool focus)
        {
            var objectUID = GetTableObjectUID(table, 0);
            if (objectUID >= 0)
            {
                SelectObjectByUID(objectUID, focus);
            }

            m_CurrentTableLink = link;
            //m_CurrentTableIndex = m_UIState.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.m_DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_EventListener.OnRepaint();
        }

        public void OpenTable(Database.TableLink link, Database.Table table, Database.CellPosition pos, bool focus)
        {
            var objectUID = GetTableObjectUID(table, pos.row);
            if (objectUID >= 0)
            {
                SelectObjectByUID(objectUID, focus);
            }

            m_CurrentTableLink = link;
            //m_CurrentTableIndex = m_UIState.GetTableIndex(table);
            m_Spreadsheet = new UI.DatabaseSpreadsheet(m_UIState.m_DataRenderer, table, this);
            m_Spreadsheet.onClickLink += OnSpreadsheetClick;
            m_Spreadsheet.Goto(pos);
            m_EventListener.OnRepaint();
        }

        public void OpenHistoryEvent(UI.HETreeMap e)
        {
            //m_TreeMap.SelectItem(a);
            //OpenMetricData(a._metric);
            if (e == null) return;
            mEventToOpenNextDraw = e;
            m_EventListener.OnRepaint();
        }

        public void OpenHistoryEventImmediate(UI.HETreeMap e)
        {
            if (m_TreeMap.HasMetric(e.selected))
            {
                OpenMetricData(e.selected, true);
            }
            else
            {
                ShowAllObjects(e.selected, true);
            }
            m_EventListener.OnRepaint();
        }

        UI.HETreeMap mEventToOpenNextDraw = null;
        public override void OnGUI(Rect r)
        {
            if (m_UIState.m_HotKey.m_CameraFocus.IsTriggered())
            {
                if (m_TreeMap.selectedItem != null)
                {
                    m_TreeMap.FocusOnItem(m_TreeMap.selectedItem, false);
                }
            }
            if (m_UIState.m_HotKey.m_CameraShowAll.IsTriggered())
            {
                if (m_TreeMap.selectedItem != null)
                {
                    m_TreeMap.FocusOnAll();
                }
            }
            m_UIState.m_DataRenderer.forceLinkAllObject = true;
            float margin = 2;
            Rect rectMap = r;
            rectMap.xMax -= kPropertyPaneWidth + margin;
            rectMap.yMax -= kDetailsPaneHeight + margin;
            m_TreeMap.OnGUI(rectMap);

            if (m_Spreadsheet != null)
            {
                Rect rectSpreadsheet = r;
                rectSpreadsheet.yMin = rectMap.yMax + margin;
                GUILayout.BeginArea(rectSpreadsheet);
                EditorGUILayout.BeginVertical();

                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(2);
                m_Spreadsheet.OnGUI(rectSpreadsheet.width);
                GUILayout.Space(2);
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(2);
                EditorGUILayout.EndVertical();
                GUILayout.EndArea();
            }

            if (mEventToOpenNextDraw != null)
            {
                //this must be done after at least one call of m_TreeMap.OnGUI(rectMap)
                //so that m_TreeMap is initialized with the appropriate rect.
                //otherwise the zoom area will generate NaNs.
                OpenHistoryEventImmediate(mEventToOpenNextDraw);
                mEventToOpenNextDraw = null;
                m_EventListener.OnRepaint();
            }
            else if (m_TreeMap.IsAnimated())
            {
                m_EventListener.OnRepaint();
            }
        }

        public override void OnClose()
        {
            m_TreeMap.CleanupMeshes();
        }
    }
}
