// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.ut
{
    internal partial struct TinyEntityInformation : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEntityInformation>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEntityInformation Construct(TinyObject tiny) => new TinyEntityInformation(tiny);
        private static TinyId s_Id = CoreIds.ut.EntityInformation;
        private static TinyType.Reference s_Ref = TypeRefs.ut.EntityInformation;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEntityInformation(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEntityInformation(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyEntityInformation other)
        {
        }
    }
}
