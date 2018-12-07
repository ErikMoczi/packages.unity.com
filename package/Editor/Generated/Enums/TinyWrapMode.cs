// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal enum TinyWrapMode
    {
        Default = 0,
        Once = 1,
        Loop = 2,
        PingPong = 4,
        ClampForever = 8,
    }

    internal class TinyWrapModeConverter : EnumTypeConverter<TinyWrapMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyWrapMode>(TypeRefs.TinyEditorExtensions.WrapMode, true);
    }
}
