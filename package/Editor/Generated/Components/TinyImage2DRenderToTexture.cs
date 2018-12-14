// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyImage2DRenderToTexture : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyImage2DRenderToTexture>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyImage2DRenderToTexture Construct(TinyObject tiny) => new TinyImage2DRenderToTexture(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Image2DRenderToTexture;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Image2DRenderToTexture;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyImage2DRenderToTexture(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyImage2DRenderToTexture(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyImage2DRenderToTexture other)
        {
        }
    }
}
