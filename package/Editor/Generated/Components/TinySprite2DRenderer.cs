// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2DRenderer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DRenderer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DRenderer Construct(TinyObject tiny) => new TinySprite2DRenderer(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2DRenderer;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2DRenderer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DRenderer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DRenderer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Sprite @sprite
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@sprite));
            set => Tiny.AssignIfDifferent(nameof(@sprite), value);
        }

        public UnityEngine.Color @color
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@color));
            set => Tiny.AssignIfDifferent(nameof(@color), value);
        }

        public Core2D.TinyBlendOp @blending
        {
            get => Tiny.GetProperty<Core2D.TinyBlendOp>(nameof(@blending));
            set => Tiny.AssignIfDifferent(nameof(@blending), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DRenderer other)
        {
            @sprite = other.@sprite;
            @color = other.@color;
            @blending = other.@blending;
        }
    }
}
