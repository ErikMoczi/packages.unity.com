// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyLifetimeScale : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyLifetimeScale>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyLifetimeScale Construct(TinyObject tiny) => new TinyLifetimeScale(tiny);
        private static TinyId s_Id = CoreIds.Particles.LifetimeScale;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.LifetimeScale;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyLifetimeScale(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyLifetimeScale(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyLifetimeScale other)
        {
            @curve = other.@curve;
        }
    }
}
