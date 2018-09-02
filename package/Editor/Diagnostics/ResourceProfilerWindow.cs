using UnityEditor;
using UnityEngine;
using EditorDiagnostics;
using System.Collections.Generic;

namespace ResourceManagement.Diagnostics
{
    public class ResourceProfilerWindow : EventViewerWindow
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
            return ((ResourceManagerEvent.Type)stream).ToString();
        }

        protected override bool OnCanHandleEvent(string graph)
        {
            return graph == ResourceManagerProfiler.EventCategory;
        }

        protected override bool OnRecordEvent(DiagnosticEvent evt)
        {
            if (evt.m_graph == ResourceManagerProfiler.EventCategory)
            {
                switch ((ResourceManagerEvent.Type)evt.m_stream)
                {
                    case ResourceManagerEvent.Type.LoadAsyncRequest:
                    case ResourceManagerEvent.Type.LoadAsyncCompletion:
                    case ResourceManagerEvent.Type.Release:
                    case ResourceManagerEvent.Type.InstantiateAsyncRequest:
                    case ResourceManagerEvent.Type.InstantiateAsyncCompletion:
                    case ResourceManagerEvent.Type.ReleaseInstance:
                    case ResourceManagerEvent.Type.LoadSceneAsyncRequest:
                    case ResourceManagerEvent.Type.LoadSceneAsyncCompletion:
                    case ResourceManagerEvent.Type.ReleaseSceneAsyncRequest:
                    case ResourceManagerEvent.Type.ReleaseSceneAsyncCompletion:
                        return true;
                }
            }
            return base.OnRecordEvent(evt);
        }

        protected override void OnEventDetailGUI(Rect rect, DiagnosticEvent evt)
        {
            if (Event.current.type != EventType.Repaint)
                return;
            var dataList = evt.m_data as List<string>;
            if (dataList == null)
                return;
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
                case 0: EditorGUI.LabelField(cellRect, ((ResourceManagerEvent.Type)evt.m_stream).ToString()); break;
                case 1: EditorGUI.LabelField(cellRect, evt.m_id); break;
                default:
                {
                    column -= 2;    //need to account for 2 columns that use build in fields
                    var dataList = evt.m_data as List<string>;
                    if (dataList == null || column >= dataList.Count)
                        return false;
                    EditorGUI.LabelField(cellRect, dataList[column]);
                }
                break;
            }

            return true;
        }

        protected override void OnInitializeGraphView(EventGraphListView gv)
        {
            gv.DefineGraph(ResourceManagerProfiler.EventCategory, (int)ResourceManagerEvent.Type.CacheEntryRefCount,
                new GraphLayerBackgroundGraph((int)ResourceManagerEvent.Type.CacheEntryLoadPercent, (int)ResourceManagerEvent.Type.CacheEntryRefCount, "LoadPercent", "Loaded", new Color(53 / 255f, 136 / 255f, 167 / 255f, .5f), new Color(53 / 255f, 136 / 255f, 167 / 255f, 1)),
                new GraphLayerBarChartMesh((int)ResourceManagerEvent.Type.CacheEntryRefCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1)),
                new GraphLayerBarChartMesh((int)ResourceManagerEvent.Type.PoolCount, "PoolSize", "Object Pool Count", new Color(204 / 255f, 113 / 255f, 0, 1)),
                new GraphLayerEventMarker((int)ResourceManagerEvent.Type.CacheEntryLoadPercent, "", "", Color.white, Color.black),
                new GraphLayerLabel((int)ResourceManagerEvent.Type.CacheEntryRefCount, "RefCount", "Reference Count", new Color(123 / 255f, 158 / 255f, 6 / 255f, 1), (v) => v.ToString())
                );
        }
    }
}
