// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterConeSource : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterConeSource>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterConeSource Construct(TinyObject tiny) => new TinyEmitterConeSource(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterConeSource;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterConeSource;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterConeSource(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterConeSource(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @radius
        {
            get => Tiny.GetProperty<float>(nameof(@radius));
            set => Tiny.AssignIfDifferent(nameof(@radius), value);
        }

        public float @angle
        {
            get => Tiny.GetProperty<float>(nameof(@angle));
            set => Tiny.AssignIfDifferent(nameof(@angle), value);
        }

        public float @speed
        {
            get => Tiny.GetProperty<float>(nameof(@speed));
            set => Tiny.AssignIfDifferent(nameof(@speed), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterConeSource other)
        {
            @radius = other.@radius;
            @angle = other.@angle;
            @speed = other.@speed;
        }
    }
}
