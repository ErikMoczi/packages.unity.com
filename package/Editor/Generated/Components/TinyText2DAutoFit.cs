// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyText2DAutoFit : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyText2DAutoFit>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyText2DAutoFit Construct(TinyObject tiny) => new TinyText2DAutoFit(tiny);
        private static TinyId s_Id = CoreIds.Text.Text2DAutoFit;
        private static TinyType.Reference s_Ref = TypeRefs.Text.Text2DAutoFit;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyText2DAutoFit(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyText2DAutoFit(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @minSize
        {
            get => Tiny.GetProperty<float>(nameof(@minSize));
            set => Tiny.AssignIfDifferent(nameof(@minSize), value);
        }

        public float @maxSize
        {
            get => Tiny.GetProperty<float>(nameof(@maxSize));
            set => Tiny.AssignIfDifferent(nameof(@maxSize), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyText2DAutoFit other)
        {
            @minSize = other.@minSize;
            @maxSize = other.@maxSize;
        }
    }
}
