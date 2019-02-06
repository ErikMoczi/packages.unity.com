// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.ut
{
    internal partial struct TinyCleanupEntity : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCleanupEntity>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCleanupEntity Construct(TinyObject tiny) => new TinyCleanupEntity(tiny);
        private static TinyId s_Id = CoreIds.ut.CleanupEntity;
        private static TinyType.Reference s_Ref = TypeRefs.ut.CleanupEntity;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCleanupEntity(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCleanupEntity(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyCleanupEntity other)
        {
        }
    }
}
