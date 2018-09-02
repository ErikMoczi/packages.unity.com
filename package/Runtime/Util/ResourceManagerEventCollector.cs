
namespace UnityEngine.ResourceManagement.Diagnostics
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
            DiagnosticEvents,
            CacheLRUCount,
            AsyncOpCacheHitRatio,
            AsyncOpCacheCount,
        }

        public static string EventCategory = "ResourceManagerEvent";
        public static void PostEvent(EventType type, object context, int eventValue)
        {
            if (!DiagnosticEventCollector.ProfileEvents)
                return;
            var parent = "";
            var id = context.ToString();
            byte[] data = null;
            var loc = context as IResourceLocation;
            if (loc != null)
            {
                id = loc.ToString();
                if (loc.HasDependencies)
                    parent = loc.Dependencies[0].ToString();
                var sb = new System.Text.StringBuilder(256);
                sb.Append(loc.ProviderId.Substring(loc.ProviderId.LastIndexOf('.') + 1));
                sb.Append('!');
                sb.Append(loc.InternalId);
                sb.Append('!');
                for (int i = 0; loc.HasDependencies && i < loc.Dependencies.Count; i++)
                {
                    sb.Append(loc.Dependencies[i].ToString());
                    sb.Append(',');
                }
                data = System.Text.Encoding.ASCII.GetBytes(sb.ToString());
            }
            var category = type >= EventType.DiagnosticEvents ? type.ToString() : EventCategory;
            DiagnosticEventCollector.PostEvent(new DiagnosticEvent(category, parent, id, (int)type, Time.frameCount, eventValue, data));
        }
    }
}
