// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterCircleSource : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterCircleSource>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterCircleSource Construct(TinyObject tiny) => new TinyEmitterCircleSource(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterCircleSource;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterCircleSource;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterCircleSource(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterCircleSource(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.Range @radius
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@radius));
            set => Tiny.AssignIfDifferent(nameof(@radius), value);
        }

        public Unity.Tiny.Range @speed
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@speed));
            set => Tiny.AssignIfDifferent(nameof(@speed), value);
        }

        public bool @speedBasedOnRadius
        {
            get => Tiny.GetProperty<bool>(nameof(@speedBasedOnRadius));
            set => Tiny.AssignIfDifferent(nameof(@speedBasedOnRadius), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterCircleSource other)
        {
            @radius = other.@radius;
            @speed = other.@speed;
            @speedBasedOnRadius = other.@speedBasedOnRadius;
        }
    }
}
