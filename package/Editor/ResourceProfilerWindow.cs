using UnityEditor;
using UnityEngine;
using EditorDiagnostics;
using System.Collections.Generic;

namespace ResourceManagement.Util
{
    /*
     * ResourceManager specific implementation of an EventViewerWindow
     */ 
    internal class ResourceProfilerWindow : EventViewerWindow
    {
        [MenuItem("Window/Resource Profiler", priority = 2051)]
        static void ShowWindow()
        {
            var window = GetWindow<ResourceProfilerWindow>();
            window.titleContent = new GUIContent("RM Profiler", "ResourceManager Profiler");
            window.Show();
        }

        protected override bool showEventDetailPanel { get { return false; } }
        protected override bool showEventPanel { get { return true; } }

        protected string GetDataStreamName(int stream)
        {
            return ((ResourceManagerEventCollector.EventType)stream).ToString();
        }

        protected override bool OnCanHandleEvent(string graph)
        {
            return graph == ResourceManagerEventCollector.EventCategory;
        }

        protected override bool OnRecordEvent(DiagnosticEvent evt)
        {
            if (evt.m_graph == ResourceManagerEventCollector.EventCategory)
            {
                switch ((ResourceManagerEventCollector.EventType)evt.m_stream)
                {
                    case ResourceManagerEventCollector.EventType.LoadAsyncRequest:
                    case ResourceManagerEventCollector.EventType.LoadAsyncCompletion:
                    case ResourceManagerEventCollector.EventType.Release:
                    case ResourceManagerEventCollector.EventType.InstantiateAsyncRequest:
                    case ResourceManagerEventCollector.EventType.InstantiateAsyncCompletion:
                    case ResourceManagerEventCollector.EventType.ReleaseInstance:
                    case ResourceManagerEventCollector.EventType.LoadSceneAsyncRequest:
                    case ResourceManagerEventCollector.EventType.LoadSceneAsyncCompletion:
                    case ResourceManagerEventCollector.EventType.ReleaseSceneAsyncRequest:
                    case ResourceManagerEventCollector.EventType.ReleaseSceneAsyncCompletion:
                        return true;
                }
            }
            return base.OnRecordEvent(evt);
        }

        protected override void OnEventDetailGUI(Rect rect, DiagnosticEvent evt)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var dataListText = System.Text.Encoding.ASCII.GetString(evt.m_data);
            if (dataListText == null)
                return;
            var dataList = dataListText.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (dataList[1].EndsWith(".bundle"))
            {
                EditorGUI.TextArea(rect, "No preview available for AssetBundle");
            }
            else
            {
                var obj = AssetDatabase.LoadAssetAtPath<GameObject>(dataList[1]);
                if (obj != null)
                {
                    var tex = AssetPreview.GetAssetPreview(obj);
                    EditorGUI.DrawPreviewTexture(rect, tex);
                }
            }
        }

        protected override void OnGetColumns(List<string> columnNames, List<float> columnSizes)
        {
            columnNames.AddRange(new string[] { "Event", "Address", "Provider", "Path", "Dependencies" });
            columnSizes.AddRange(new float[] { 150, 150, 200, 300, 400});
        }

        protected override bool OnColumnCellGUI(Rect cellRect, DiagnosticEvent evt, int column)
        {
            switch (column)
            {
                case 0: EditorGUI.LabelField(cellRect, ((ResourceManagerEventCollector.EventType)evt.m_stream).ToString()); break;
                case 1: EditorGUI.LabelField(cellRect, evt.m_id); break;
                default:
                {
                    column -= 2;    //need to account for 2 columns that use build in fields
                        if (evt.m_data != null && evt.m_data.Length > 0)
                        {
                            var dataListText = System.Text.Encoding.ASCII.GetString(evt.m_data);
                            if (dataListText == null)
                                return false;
                            var dataList = dataListText.Split(new char[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries);
                            if (dataList == null || column >= dataList.Length)
                                return false;
                            EditorGUI.LabelField(cellRect, dataList[column]);
                        }
                }
                break;
            }

            return true;
        }

        protected override void OnInitializeGraphView(EventGraphListView gv)
        {
            gv.DefineGraph(ResourceManagerEventCollector.EventCategory, (int)ResourceManagerEventCollector.EventType.CacheEntryRefCount,
                new GraphLayerBackgroundGraph((int)ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, (int)ResourceManagerEventCollector.EventType.CacheEntryRefCount, "LoadPercent", "Loaded", new Color(53 / 255f, 136 / 255f, 167 / 255f, .5f), new Color(53 / 255f, 136 / 255f, 167 / 255f, 1)),
                new GraphLayerBarChartMesh((int)ResourceManagerEventCollector.EventType.CacheEntryRefCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1)),
                new GraphLayerBarChartMesh((int)ResourceManagerEventCollector.EventType.PoolCount, "PoolSize", "Object Pool Count", new Color(204 / 255f, 113 / 255f, 0, 1)),
                new GraphLayerEventMarker((int)ResourceManagerEventCollector.EventType.CacheEntryLoadPercent, "", "", Color.white, Color.black),
                new GraphLayerLabel((int)ResourceManagerEventCollector.EventType.CacheEntryRefCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1), (v) => v.ToString())
                );
        }
    }
}
