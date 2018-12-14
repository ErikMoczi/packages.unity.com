// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.ut
{
    internal enum TinyAccessMode
    {
        ReadWrite = 0,
        ReadOnly = 1,
        Subtractive = 2,
        AnyOfReadWrite = 3,
        AnyOfReadOnly = 4,
        OptionalReadWrite = 5,
        OptionalReadOnly = 6,
    }

    internal class TinyAccessModeConverter : EnumTypeConverter<TinyAccessMode>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyAccessMode>(TypeRefs.ut.AccessMode, true);
    }
}
