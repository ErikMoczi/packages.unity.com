// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyRectTransformFinalSize : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyRectTransformFinalSize>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyRectTransformFinalSize Construct(TinyObject tiny) => new TinyRectTransformFinalSize(tiny);
        private static TinyId s_Id = CoreIds.Core2D.RectTransformFinalSize;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.RectTransformFinalSize;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRectTransformFinalSize(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyRectTransformFinalSize(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyRectTransformFinalSize other)
        {
        }
    }
}
