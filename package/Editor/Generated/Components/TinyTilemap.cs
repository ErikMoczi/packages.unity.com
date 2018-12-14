// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tilemap2D
{
    internal partial struct TinyTilemap : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTilemap>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTilemap Construct(TinyObject tiny) => new TinyTilemap(tiny);
        private static TinyId s_Id = CoreIds.Tilemap2D.Tilemap;
        private static TinyType.Reference s_Ref = TypeRefs.Tilemap2D.Tilemap;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTilemap(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTilemap(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @anchor
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@anchor));
            set => Tiny.AssignIfDifferent(nameof(@anchor), value);
        }

        public UnityEngine.Vector3 @position
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@position));
            set => Tiny.AssignIfDifferent(nameof(@position), value);
        }

        public UnityEngine.Quaternion @rotation
        {
            get => Tiny.GetProperty<UnityEngine.Quaternion>(nameof(@rotation));
            set => Tiny.AssignIfDifferent(nameof(@rotation), value);
        }

        public UnityEngine.Vector3 @scale
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@scale));
            set => Tiny.AssignIfDifferent(nameof(@scale), value);
        }

        public UnityEngine.Vector3 @cellSize
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@cellSize));
            set => Tiny.AssignIfDifferent(nameof(@cellSize), value);
        }

        public UnityEngine.Vector3 @cellGap
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@cellGap));
            set => Tiny.AssignIfDifferent(nameof(@cellGap), value);
        }

        public TinyList @tiles
        {
            get => Tiny[nameof(@tiles)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyTilemap other)
        {
            @anchor = other.@anchor;
            @position = other.@position;
            @rotation = other.@rotation;
            @scale = other.@scale;
            @cellSize = other.@cellSize;
            @cellGap = other.@cellGap;
            CopyList(@tiles, other.@tiles);
        }
        private void CopyList(TinyList lhs, TinyList rhs)
        {
            lhs.Clear();
            foreach (var item in rhs)
            {
                lhs.Add(item);
            }
        }
    }
}
