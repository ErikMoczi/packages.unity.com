// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyLifetimeVelocity : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyLifetimeVelocity>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyLifetimeVelocity Construct(TinyObject tiny) => new TinyLifetimeVelocity(tiny);
        private static TinyId s_Id = CoreIds.Particles.LifetimeVelocity;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.LifetimeVelocity;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyLifetimeVelocity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyLifetimeVelocity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyEditorExtensions.TinyCurve3Entity @curve
        {
            get => new TinyEditorExtensions.TinyCurve3Entity(Tiny[nameof(@curve)] as TinyObject);
            set => new TinyEditorExtensions.TinyCurve3Entity(Tiny[nameof(@curve)] as TinyObject).CopyFrom(value);
        }

        #endregion // Properties

        public void CopyFrom(TinyLifetimeVelocity other)
        {
            @curve = other.@curve;
        }
    }
}
