// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyCameraCullingMode
    {
        None = 0,
        All = 1,
        Any = 2,
    }

    internal class TinyCameraCullingModeConverter : EnumTypeConverter<TinyCameraCullingMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyCameraCullingMode>(TypeRefs.Core2D.CameraCullingMode, true);
    }
}
