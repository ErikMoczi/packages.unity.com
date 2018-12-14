// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTilemapRechunk : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTilemapRechunk>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTilemapRechunk Construct(TinyObject tiny) => new TinyTilemapRechunk(tiny);
        private static TinyId s_Id = CoreIds.Tilemap2D.TilemapRechunk;
        private static TinyType.Reference s_Ref = TypeRefs.Tilemap2D.TilemapRechunk;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTilemapRechunk(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTilemapRechunk(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyTilemapRechunk other)
        {
        }
    }
}
