// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyEmitterInitialColor : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEmitterInitialColor>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEmitterInitialColor Construct(TinyObject tiny) => new TinyEmitterInitialColor(tiny);
        private static TinyId s_Id = CoreIds.Particles.EmitterInitialColor;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.EmitterInitialColor;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEmitterInitialColor(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEmitterInitialColor(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Gradient @curve
        {
            get => Tiny.GetProperty<UnityEngine.Gradient>(nameof(@curve));
            set => Tiny.AssignIfDifferent(nameof(@curve), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEmitterInitialColor other)
        {
            @curve = other.@curve;
        }
    }
}
