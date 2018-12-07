// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2DRendererOptions : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DRendererOptions>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DRendererOptions Construct(TinyObject tiny) => new TinySprite2DRendererOptions(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2DRendererOptions;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2DRendererOptions;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DRendererOptions(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DRendererOptions(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @size
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@size));
            set => Tiny.AssignIfDifferent(nameof(@size), value);
        }

        public Unity.Tiny.DrawMode @drawMode
        {
            get => Tiny.GetProperty<Unity.Tiny.DrawMode>(nameof(@drawMode));
            set => Tiny.AssignIfDifferent(nameof(@drawMode), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DRendererOptions other)
        {
            @size = other.@size;
            @drawMode = other.@drawMode;
        }
    }
}
