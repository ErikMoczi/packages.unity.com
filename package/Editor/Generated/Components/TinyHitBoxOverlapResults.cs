// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.HitBox2D
{
    internal partial struct TinyHitBoxOverlapResults : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyHitBoxOverlapResults>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyHitBoxOverlapResults Construct(TinyObject tiny) => new TinyHitBoxOverlapResults(tiny);
        private static TinyId s_Id = CoreIds.HitBox2D.HitBoxOverlapResults;
        private static TinyType.Reference s_Ref = TypeRefs.HitBox2D.HitBoxOverlapResults;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyHitBoxOverlapResults(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyHitBoxOverlapResults(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyHitBoxOverlapResults other)
        {
        }
    }
}
