// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.Text
{
    internal enum TinyFontName
    {
        SansSerif = 0,
        Serif = 1,
        Monospace = 2,
    }

    internal class TinyFontNameConverter : EnumTypeConverter<TinyFontName>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyFontName>(TypeRefs.Text.FontName, true);
    }
}
