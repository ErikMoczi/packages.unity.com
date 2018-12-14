// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyBlendOp
    {
        Alpha = 0,
        Add = 1,
        Multiply = 2,
        MultiplyAlpha = 3,
    }

    internal class TinyBlendOpConverter : EnumTypeConverter<TinyBlendOp>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyBlendOp>(TypeRefs.Core2D.BlendOp, true);
    }
}
