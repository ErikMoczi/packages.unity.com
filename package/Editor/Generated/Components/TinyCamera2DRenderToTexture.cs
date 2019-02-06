// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyCamera2DRenderToTexture : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCamera2DRenderToTexture>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCamera2DRenderToTexture Construct(TinyObject tiny) => new TinyCamera2DRenderToTexture(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Camera2DRenderToTexture;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Camera2DRenderToTexture;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCamera2DRenderToTexture(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCamera2DRenderToTexture(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @width
        {
            get => Tiny.GetProperty<int>(nameof(@width));
            set => Tiny.AssignIfDifferent(nameof(@width), value);
        }

        public int @height
        {
            get => Tiny.GetProperty<int>(nameof(@height));
            set => Tiny.AssignIfDifferent(nameof(@height), value);
        }

        public Unity.Tiny.TinyEntity.Reference @target
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@target));
            set => Tiny.AssignIfDifferent(nameof(@target), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCamera2DRenderToTexture other)
        {
            @width = other.@width;
            @height = other.@height;
            @target = other.@target;
        }
    }
}
