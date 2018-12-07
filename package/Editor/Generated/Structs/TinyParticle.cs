// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyParticle : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Particles.Particle;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.Particle;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyParticle(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyParticle(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @time
        {
            get => Tiny.GetProperty<float>(nameof(@time));
            set => Tiny.AssignIfDifferent(nameof(@time), value);
        }

        public float @lifetime
        {
            get => Tiny.GetProperty<float>(nameof(@lifetime));
            set => Tiny.AssignIfDifferent(nameof(@lifetime), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyParticle other)
        {
            @time = other.@time;
            @lifetime = other.@lifetime;
        }
    }
}
