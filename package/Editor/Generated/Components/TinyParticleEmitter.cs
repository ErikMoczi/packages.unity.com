// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyParticleEmitter : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyParticleEmitter>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyParticleEmitter Construct(TinyObject tiny) => new TinyParticleEmitter(tiny);
        private static TinyId s_Id = CoreIds.Particles.ParticleEmitter;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.ParticleEmitter;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyParticleEmitter(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyParticleEmitter(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @particle
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@particle));
            set => Tiny.AssignIfDifferent(nameof(@particle), value);
        }

        public uint @maxParticles
        {
            get => Tiny.GetProperty<uint>(nameof(@maxParticles));
            set => Tiny.AssignIfDifferent(nameof(@maxParticles), value);
        }

        public float @emitRate
        {
            get => Tiny.GetProperty<float>(nameof(@emitRate));
            set => Tiny.AssignIfDifferent(nameof(@emitRate), value);
        }

        public Unity.Tiny.Range @lifetime
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@lifetime));
            set => Tiny.AssignIfDifferent(nameof(@lifetime), value);
        }

        public bool @attachToEmitter
        {
            get => Tiny.GetProperty<bool>(nameof(@attachToEmitter));
            set => Tiny.AssignIfDifferent(nameof(@attachToEmitter), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyParticleEmitter other)
        {
            @particle = other.@particle;
            @maxParticles = other.@maxParticles;
            @emitRate = other.@emitRate;
            @lifetime = other.@lifetime;
            @attachToEmitter = other.@attachToEmitter;
        }
    }
}
