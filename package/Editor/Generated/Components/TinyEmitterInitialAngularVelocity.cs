// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterInitialAngularVelocity : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterInitialAngularVelocity>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterInitialAngularVelocity Construct(TinyObject tiny) => new TinyEmitterInitialAngularVelocity(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterInitialAngularVelocity;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterInitialAngularVelocity;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterInitialAngularVelocity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterInitialAngularVelocity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.Range @angularVelocity
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@angularVelocity));
            set => Tiny.AssignIfDifferent(nameof(@angularVelocity), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterInitialAngularVelocity other)
        {
            @angularVelocity = other.@angularVelocity;
        }
    }
}
