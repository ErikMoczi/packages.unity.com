// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Core2D
{
    internal enum TinyImage2DMemoryFormat
    {
        RGBA8Premultiplied = 0,
        RGBA8 = 1,
        A8 = 2,
    }

    internal class TinyImage2DMemoryFormatConverter : EnumTypeConverter<TinyImage2DMemoryFormat>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyImage2DMemoryFormat>(TypeRefs.Core2D.Image2DMemoryFormat, true);
    }
}
