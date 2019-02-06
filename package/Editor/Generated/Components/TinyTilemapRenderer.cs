// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTilemapRenderer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTilemapRenderer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTilemapRenderer Construct(TinyObject tiny) => new TinyTilemapRenderer(tiny);
        private static TinyId s_Id = CoreIds.Tilemap2D.TilemapRenderer;
        private static TinyType.Reference s_Ref = TypeRefs.Tilemap2D.TilemapRenderer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTilemapRenderer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTilemapRenderer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Tilemaps.Tilemap @tilemap
        {
            get => Tiny.GetProperty<UnityEngine.Tilemaps.Tilemap>(nameof(@tilemap));
            set => Tiny.AssignIfDifferent(nameof(@tilemap), value);
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

        public void CopyFrom(TinyTilemapRenderer other)
        {
            @tilemap = other.@tilemap;
            @color = other.@color;
            @blending = other.@blending;
        }
    }
}
