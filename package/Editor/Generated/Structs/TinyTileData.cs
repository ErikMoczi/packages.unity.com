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

        public UnityEngine.Tilemaps.Tile @tile
        {
            get => Tiny.GetProperty<UnityEngine.Tilemaps.Tile>(nameof(@tile));
            set => Tiny.AssignIfDifferent(nameof(@tile), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTileData other)
        {
            @position = other.@position;
            @tile = other.@tile;
        }
    }
}
