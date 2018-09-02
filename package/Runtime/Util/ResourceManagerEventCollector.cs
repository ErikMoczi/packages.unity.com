
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
        static string PrettyPath(string p, bool keepExtension)
        {
            var slashIndex = p.LastIndexOf('/');
            if (slashIndex > 0)
                p = p.Substring(slashIndex + 1);
            if (!keepExtension)
            {
                slashIndex = p.LastIndexOf('.');
                if (slashIndex > 0)
                    p = p.Substring(0, slashIndex);
            }
            return p;
        }

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
                id = PrettyPath(loc.InternalId, false);
                var sb = new System.Text.StringBuilder(256);
                sb.Append(loc.ProviderId.Substring(loc.ProviderId.LastIndexOf('.') + 1));
                sb.Append('!');
                sb.Append(loc.InternalId);
                sb.Append('!');
                if (loc.HasDependencies)
                {
                    parent = PrettyPath(loc.Dependencies[0].InternalId, false);
                    for (int i = 0; i < loc.Dependencies.Count; i++)
                    {
                        sb.Append(PrettyPath(loc.Dependencies[i].InternalId, true));
                        sb.Append(',');
                    }
                }
                data = System.Text.Encoding.ASCII.GetBytes(sb.ToString());
            }
            var category = type >= EventType.DiagnosticEvents ? type.ToString() : EventCategory;
            DiagnosticEventCollector.PostEvent(new DiagnosticEvent(category, parent, id, (int)type, Time.frameCount, eventValue, data));
        }
    }
}
