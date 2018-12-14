// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenVector2 : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenVector2>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenVector2 Construct(TinyObject tiny) => new TinyTweenVector2(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenVector2;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenVector2;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenVector2(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenVector2(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @start
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public UnityEngine.Vector2 @end
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenVector2 other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
