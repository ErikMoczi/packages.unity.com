// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTile : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTile>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTile Construct(TinyObject tiny) => new TinyTile(tiny);
        private static TinyId s_Id = CoreIds.Tilemap2D.Tile;
        private static TinyType.Reference s_Ref = TypeRefs.Tilemap2D.Tile;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTile(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTile(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Color @color
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@color));
            set => Tiny.AssignIfDifferent(nameof(@color), value);
        }

        public UnityEngine.Sprite @sprite
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@sprite));
            set => Tiny.AssignIfDifferent(nameof(@sprite), value);
        }

        public UnityEngine.Tilemaps.Tile.ColliderType @colliderType
        {
            get => Tiny.GetProperty<UnityEngine.Tilemaps.Tile.ColliderType>(nameof(@colliderType));
            set => Tiny.AssignIfDifferent(nameof(@colliderType), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTile other)
        {
            @color = other.@color;
            @sprite = other.@sprite;
            @colliderType = other.@colliderType;
        }
    }
}
