// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal enum TinyGradientMode
    {
        Blend = 0,
        Fixed = 1,
    }

    internal class TinyGradientModeConverter : EnumTypeConverter<TinyGradientMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyGradientMode>(TypeRefs.TinyEditorExtensions.GradientMode, true);
    }
}
