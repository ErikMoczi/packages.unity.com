// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Tweens
{
    internal enum TinyTweenFunc
    {
        External = 0,
        Linear = 1,
        Hardstep = 2,
        Smoothstep = 3,
        Cosine = 4,
        InQuad = 5,
        OutQuad = 6,
        InOutQuad = 7,
        InCubic = 8,
        OutCubic = 9,
        InOutCubic = 10,
        InQuart = 11,
        OutQuart = 12,
        InOutQuart = 13,
        InQuint = 14,
        OutQuint = 15,
        InOutQuint = 16,
        InBack = 17,
        OutBack = 18,
        InOutBack = 19,
        InBounce = 20,
        OutBounce = 21,
        InOutBounce = 22,
        InCircle = 23,
        OutCircle = 24,
        InOutCircle = 25,
        InExponential = 26,
        OutExponential = 27,
        InOutExponential = 28,
    }

    internal class TinyTweenFuncConverter : EnumTypeConverter<TinyTweenFunc>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyTweenFunc>(TypeRefs.Tweens.TweenFunc, true);
    }
}
