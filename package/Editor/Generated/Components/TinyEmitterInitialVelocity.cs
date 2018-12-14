// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterInitialVelocity : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterInitialVelocity>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterInitialVelocity Construct(TinyObject tiny) => new TinyEmitterInitialVelocity(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterInitialVelocity;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterInitialVelocity;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterInitialVelocity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterInitialVelocity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @velocity
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@velocity));
            set => Tiny.AssignIfDifferent(nameof(@velocity), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterInitialVelocity other)
        {
            @velocity = other.@velocity;
        }
    }
}
