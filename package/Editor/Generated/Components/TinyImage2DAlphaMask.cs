// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyImage2DAlphaMask : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyImage2DAlphaMask>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyImage2DAlphaMask Construct(TinyObject tiny) => new TinyImage2DAlphaMask(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Image2DAlphaMask;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Image2DAlphaMask;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyImage2DAlphaMask(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyImage2DAlphaMask(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @threshold
        {
            get => Tiny.GetProperty<float>(nameof(@threshold));
            set => Tiny.AssignIfDifferent(nameof(@threshold), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyImage2DAlphaMask other)
        {
            @threshold = other.@threshold;
        }
    }
}
