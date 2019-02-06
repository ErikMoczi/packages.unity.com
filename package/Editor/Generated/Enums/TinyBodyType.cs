// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Physics2D
{
    internal enum TinyBodyType
    {
        Static = 0,
        Kinematic = 1,
        Dynamic = 2,
        BulletDynamic = 3,
    }

    internal class TinyBodyTypeConverter : EnumTypeConverter<TinyBodyType>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyBodyType>(TypeRefs.Physics2D.BodyType, true);
    }
}
