// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Video
{
    internal enum TinyVideoClipLoadingStatus
    {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2,
        LoadError = 3,
    }

    internal class TinyVideoClipLoadingStatusConverter : EnumTypeConverter<TinyVideoClipLoadingStatus>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyVideoClipLoadingStatus>(TypeRefs.Video.VideoClipLoadingStatus, true);
    }
}
