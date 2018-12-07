// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenVector3 : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenVector3>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenVector3 Construct(TinyObject tiny) => new TinyTweenVector3(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenVector3;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenVector3;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenVector3(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenVector3(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @start
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public UnityEngine.Vector3 @end
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenVector3 other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
