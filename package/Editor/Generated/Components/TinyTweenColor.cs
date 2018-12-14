// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenColor : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenColor>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenColor Construct(TinyObject tiny) => new TinyTweenColor(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenColor;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenColor;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenColor(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenColor(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @start
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public UnityEngine.Color @end
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenColor other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
