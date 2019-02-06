// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTileData : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Tilemap2D.TileData;
        private static TinyType.Reference s_Ref = TypeRefs.Tilemap2D.TileData;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTileData(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyTileData(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @position
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@position));
            set => Tiny.AssignIfDifferent(nameof(@position), value);
        }

        public UnityEngine.Tilemaps.TileBase @tile
        {
            get => Tiny.GetProperty<UnityEngine.Tilemaps.TileBase>(nameof(@tile));
            set => Tiny.AssignIfDifferent(nameof(@tile), value);
        }

        public UnityEngine.Sprite @bakedSprite
        {
            get => Tiny.GetProperty<UnityEngine.Sprite>(nameof(@bakedSprite));
            set => Tiny.AssignIfDifferent(nameof(@bakedSprite), value);
        }

        public UnityEngine.Color @bakedColor
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@bakedColor));
            set => Tiny.AssignIfDifferent(nameof(@bakedColor), value);
        }

        public UnityEngine.Tilemaps.Tile.ColliderType @bakedColliderType
        {
            get => Tiny.GetProperty<UnityEngine.Tilemaps.Tile.ColliderType>(nameof(@bakedColliderType));
            set => Tiny.AssignIfDifferent(nameof(@bakedColliderType), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTileData other)
        {
            @position = other.@position;
            @tile = other.@tile;
            @bakedSprite = other.@bakedSprite;
            @bakedColor = other.@bakedColor;
            @bakedColliderType = other.@bakedColliderType;
        }
    }
}
