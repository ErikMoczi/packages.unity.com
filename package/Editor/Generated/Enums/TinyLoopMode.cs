// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyLoopMode
    {
        Loop = 0,
        Once = 1,
        PingPong = 2,
        PingPongOnce = 3,
        ClampForever = 4,
    }

    internal class TinyLoopModeConverter : EnumTypeConverter<TinyLoopMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyLoopMode>(TypeRefs.Core2D.LoopMode, true);
    }
}
