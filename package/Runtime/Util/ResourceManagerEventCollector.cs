namespace ResourceManagement.Util
{
    public static class ResourceManagerEventCollector
    {
        public enum EventType
        {
            None,
            FrameCount,
            LoadAsyncRequest,
            LoadAsyncCompletion,
            Release,
            InstantiateAsyncRequest,
            InstantiateAsyncCompletion,
            ReleaseInstance,
            LoadSceneAsyncRequest,
            LoadSceneAsyncCompletion,
            ReleaseSceneAsyncRequest,
            ReleaseSceneAsyncCompletion,
            CacheEntryRefCount,
            CacheEntryLoadPercent,
            PoolCount,
        }

        public static string EventCategory = "ResourceManagerEvent";
        public static void PostEvent(ResourceManagerEventCollector.EventType type, IResourceLocation loc, int val)
        {
            if (!ResourceManager.m_postEvents)
                return;
            var parent = "";
            var id = "";
            byte[] data = null;
            if (loc != null)
            {
                id = loc.ToString();
                if (loc.dependencies != null && loc.dependencies.Count > 0)
                    parent = loc.dependencies[0].ToString();
                var sb = new System.Text.StringBuilder(256);
                sb.Append(loc.providerId.Substring(loc.providerId.LastIndexOf('.') + 1));
                sb.Append(',');
                sb.Append(loc.id);
                sb.Append(',');
                for (int i = 0; loc.dependencies != null && i < loc.dependencies.Count; i++)
                {
                    sb.Append(loc.dependencies[i].ToString());
                    sb.Append(',');
                }
                data = System.Text.Encoding.ASCII.GetBytes(sb.ToString());
            }
            EditorDiagnostics.EventCollector.PostEvent(
                new EditorDiagnostics.DiagnosticEvent(EventCategory, parent, id, (int)type, UnityEngine.Time.frameCount, val, data));
        }
    }
}
