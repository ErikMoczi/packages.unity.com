// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Audio
{
    internal enum TinyAudioClipStatus
    {
        Unloaded = 0,
        Loading = 1,
        Loaded = 2,
        LoadError = 3,
    }

    internal class TinyAudioClipStatusConverter : EnumTypeConverter<TinyAudioClipStatus>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyAudioClipStatus>(TypeRefs.Audio.AudioClipStatus, true);
    }
}
