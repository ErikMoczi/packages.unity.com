
namespace Unity.Tiny
{
    internal interface IGUIVisitorStateTracker
    {
        void CacheState();
        void RestoreState();
    }
}
