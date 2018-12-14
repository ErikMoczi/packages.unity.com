// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterInitialScale : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterInitialScale>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterInitialScale Construct(TinyObject tiny) => new TinyEmitterInitialScale(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterInitialScale;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterInitialScale;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterInitialScale(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterInitialScale(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.Range @scale
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@scale));
            set => Tiny.AssignIfDifferent(nameof(@scale), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterInitialScale other)
        {
            @scale = other.@scale;
        }
    }
}
