// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyDisplayOrientation
    {
        Horizontal = 0,
        Vertical = 1,
    }

    internal class TinyDisplayOrientationConverter : EnumTypeConverter<TinyDisplayOrientation>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyDisplayOrientation>(TypeRefs.Core2D.DisplayOrientation, true);
    }
}
