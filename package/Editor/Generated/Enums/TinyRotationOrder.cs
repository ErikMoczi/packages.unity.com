// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Math
{
    internal enum TinyRotationOrder
    {
        XYZ = 0,
        XZY = 1,
        YZX = 2,
        YXZ = 3,
        ZXY = 4,
        Default = 4,
        ZYX = 5,
    }

    internal class TinyRotationOrderConverter : EnumTypeConverter<TinyRotationOrder>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyRotationOrder>(TypeRefs.Math.RotationOrder, true);
    }
}
