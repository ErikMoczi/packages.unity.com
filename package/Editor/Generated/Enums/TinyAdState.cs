// TINY GENERATED CODE, DO NOT EDIT BY HAND



namespace Unity.Tiny.Runtime.PlayableAd
{
    internal enum TinyAdState
    {
        Hidden = 0,
        Default = 1,
        Loading = 2,
        Expanded = 3,
    }

    internal class TinyAdStateConverter : EnumTypeConverter<TinyAdState>
    {
        [TinyCachable] public static void Register(ICacheManager cacheManager) => cacheManager.RegisterConverter<TinyAdState>(TypeRefs.PlayableAd.AdState, true);
    }
}
