// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterBoxSource : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterBoxSource>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterBoxSource Construct(TinyObject tiny) => new TinyEmitterBoxSource(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterBoxSource;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterBoxSource;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterBoxSource(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterBoxSource(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Rect @rect
        {
            get => Tiny.GetProperty<UnityEngine.Rect>(nameof(@rect));
            set => Tiny.AssignIfDifferent(nameof(@rect), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterBoxSource other)
        {
            @rect = other.@rect;
        }
    }
}
