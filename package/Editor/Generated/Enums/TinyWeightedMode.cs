// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.TinyEditorExtensions
{
    internal enum TinyWeightedMode
    {
        None = 0,
        In = 1,
        Out = 2,
        Both = 3,
    }

    internal class TinyWeightedModeConverter : EnumTypeConverter<TinyWeightedMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyWeightedMode>(TypeRefs.TinyEditorExtensions.WeightedMode, true);
    }
}
