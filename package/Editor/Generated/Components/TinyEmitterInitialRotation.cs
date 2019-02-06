// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterInitialRotation : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterInitialRotation>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterInitialRotation Construct(TinyObject tiny) => new TinyEmitterInitialRotation(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterInitialRotation;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterInitialRotation;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterInitialRotation(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterInitialRotation(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.Range @angle
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@angle));
            set => Tiny.AssignIfDifferent(nameof(@angle), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterInitialRotation other)
        {
            @angle = other.@angle;
        }
    }
}
