// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyImage2DSaveStatus
    {
        Invalid = 0,
        Written = 1,
        Writing = 2,
        WriteErrorBadInput = 3,
        WriteErrorUnsuportedFormat = 4,
        WriteError = 5,
    }

    internal class TinyImage2DSaveStatusConverter : EnumTypeConverter<TinyImage2DSaveStatus>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyImage2DSaveStatus>(TypeRefs.Core2D.Image2DSaveStatus, true);
    }
}
