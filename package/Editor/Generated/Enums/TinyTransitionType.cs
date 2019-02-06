// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.UIControlsExtensions
{
    internal enum TinyTransitionType
    {
        ColorTint = 0,
        Sprite = 1,
    }

    internal class TinyTransitionTypeConverter : EnumTypeConverter<TinyTransitionType>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyTransitionType>(TypeRefs.UIControlsExtensions.TransitionType, true);
    }
}
