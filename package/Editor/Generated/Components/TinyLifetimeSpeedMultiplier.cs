// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyLifetimeSpeedMultiplier : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyLifetimeSpeedMultiplier>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyLifetimeSpeedMultiplier Construct(TinyObject tiny) => new TinyLifetimeSpeedMultiplier(tiny);
        private static TinyId s_Id = CoreIds.Particles.LifetimeSpeedMultiplier;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.LifetimeSpeedMultiplier;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyLifetimeSpeedMultiplier(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyLifetimeSpeedMultiplier(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.AnimationCurve @curve
        {
            get => Tiny.GetProperty<UnityEngine.AnimationCurve>(nameof(@curve));
            set => Tiny.AssignIfDifferent(nameof(@curve), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyLifetimeSpeedMultiplier other)
        {
            @curve = other.@curve;
        }
    }
}
