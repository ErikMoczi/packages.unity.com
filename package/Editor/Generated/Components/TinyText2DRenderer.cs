// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyText2DRenderer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyText2DRenderer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyText2DRenderer Construct(TinyObject tiny) => new TinyText2DRenderer(tiny);
        private static TinyId s_Id = CoreIds.Text.Text2DRenderer;
        private static TinyType.Reference s_Ref = TypeRefs.Text.Text2DRenderer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyText2DRenderer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyText2DRenderer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @style
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@style));
            set => Tiny.AssignIfDifferent(nameof(@style), value);
        }

        public string @text
        {
            get => Tiny.GetProperty<string>(nameof(@text));
            set => Tiny.AssignIfDifferent(nameof(@text), value);
        }

        public UnityEngine.Vector2 @pivot
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@pivot));
            set => Tiny.AssignIfDifferent(nameof(@pivot), value);
        }

        public Core2D.TinyBlendOp @blending
        {
            get => Tiny.GetProperty<Core2D.TinyBlendOp>(nameof(@blending));
            set => Tiny.AssignIfDifferent(nameof(@blending), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyText2DRenderer other)
        {
            @style = other.@style;
            @text = other.@text;
            @pivot = other.@pivot;
            @blending = other.@blending;
        }
    }
}
