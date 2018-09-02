using System.Collections.Generic;

namespace ResourceManagement.Diagnostics
{
    public static class ResourceManagerProfiler
    {
        public static string EventCategory = "ResourceManagerEvent";
        public static void PostEvent(ResourceManagerEvent.Type type, IResourceLocation loc, int val)
        {
            var parent = "";
            var id = "";
            object data = null;
            if (loc != null)
            {
                id = loc.ToString();
                if (loc.dependencies != null && loc.dependencies.Count > 0)
                    parent = loc.dependencies[0].ToString();
                var dataList = new List<string>();
                dataList.Add(loc.providerId.Substring(loc.providerId.LastIndexOf('.') + 1));
                dataList.Add(loc.id);
                for (int i = 0; loc.dependencies != null && i < loc.dependencies.Count; i++)
                    dataList.Add(loc.dependencies[i].ToString());
                data = dataList;
            }
            EditorDiagnostics.EventCollector.PostEvent(
                new EditorDiagnostics.DiagnosticEvent(EventCategory, parent, id, (int)type, UnityEngine.Time.frameCount, val, data));
        }
    }
}
