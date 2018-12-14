// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyTouchState
    {
        Began = 0,
        Moved = 1,
        Stationary = 2,
        Ended = 3,
        Canceled = 4,
    }

    internal class TinyTouchStateConverter : EnumTypeConverter<TinyTouchState>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyTouchState>(TypeRefs.Core2D.TouchState, true);
    }
}
