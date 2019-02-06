// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTransformStatic : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTransformStatic>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTransformStatic Construct(TinyObject tiny) => new TinyTransformStatic(tiny);
        private static TinyId s_Id = CoreIds.Core2D.TransformStatic;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.TransformStatic;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransformStatic(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTransformStatic(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyTransformStatic other)
        {
        }
    }
}
