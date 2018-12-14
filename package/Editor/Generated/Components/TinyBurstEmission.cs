// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Particles
{
    internal partial struct TinyBurstEmission : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyBurstEmission>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyBurstEmission Construct(TinyObject tiny) => new TinyBurstEmission(tiny);
        private static TinyId s_Id = CoreIds.Particles.BurstEmission;
        private static TinyType.Reference s_Ref = TypeRefs.Particles.BurstEmission;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyBurstEmission(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyBurstEmission(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.Range @count
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@count));
            set => Tiny.AssignIfDifferent(nameof(@count), value);
        }

        public Unity.Tiny.Range @interval
        {
            get => Tiny.GetProperty<Unity.Tiny.Range>(nameof(@interval));
            set => Tiny.AssignIfDifferent(nameof(@interval), value);
        }

        public int @cycles
        {
            get => Tiny.GetProperty<int>(nameof(@cycles));
            set => Tiny.AssignIfDifferent(nameof(@cycles), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyBurstEmission other)
        {
            @count = other.@count;
            @interval = other.@interval;
            @cycles = other.@cycles;
        }
    }
}
