// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Physics2D
{
    internal partial struct TinySetVelocity2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySetVelocity2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySetVelocity2D Construct(TinyObject tiny) => new TinySetVelocity2D(tiny);
        private static TinyId s_Id = CoreIds.Physics2D.SetVelocity2D;
        private static TinyType.Reference s_Ref = TypeRefs.Physics2D.SetVelocity2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySetVelocity2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySetVelocity2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinySetVelocity2D other)
        {
        }
    }
}
