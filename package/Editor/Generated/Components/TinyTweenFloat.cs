// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenFloat : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenFloat>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenFloat Construct(TinyObject tiny) => new TinyTweenFloat(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenFloat;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenFloat;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenFloat(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenFloat(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @start
        {
            get => Tiny.GetProperty<float>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public float @end
        {
            get => Tiny.GetProperty<float>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenFloat other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
