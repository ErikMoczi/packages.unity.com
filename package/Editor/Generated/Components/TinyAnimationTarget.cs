// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationTarget : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAnimationTarget>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAnimationTarget Construct(TinyObject tiny) => new TinyAnimationTarget(tiny);
        private static TinyId s_Id = CoreIds.Animation.AnimationTarget;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationTarget;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationTarget(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAnimationTarget(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @target
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@target));
            set => Tiny.AssignIfDifferent(nameof(@target), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationTarget other)
        {
            @target = other.@target;
        }
    }
}
