// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.ut
{
    internal partial struct TinyDisabled : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyDisabled>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyDisabled Construct(TinyObject tiny) => new TinyDisabled(tiny);
        private static TinyId s_Id = CoreIds.ut.Disabled;
        private static TinyType.Reference s_Ref = TypeRefs.ut.Disabled;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyDisabled(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyDisabled(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyDisabled other)
        {
        }
    }
}
