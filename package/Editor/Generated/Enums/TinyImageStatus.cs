// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyImageStatus
    {
        Invalid = 0,
        Loaded = 1,
        Loading = 2,
        LoadError = 3,
    }

    internal class TinyImageStatusConverter : EnumTypeConverter<TinyImageStatus>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyImageStatus>(TypeRefs.Core2D.ImageStatus, true);
    }
}
