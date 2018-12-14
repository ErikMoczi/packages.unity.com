// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyAssetReferenceSpriteAtlas : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAssetReferenceSpriteAtlas>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAssetReferenceSpriteAtlas Construct(TinyObject tiny) => new TinyAssetReferenceSpriteAtlas(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.AssetReferenceSpriteAtlas;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.AssetReferenceSpriteAtlas;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAssetReferenceSpriteAtlas(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAssetReferenceSpriteAtlas(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @guid
        {
            get => Tiny.GetProperty<string>(nameof(@guid));
            set => Tiny.AssignIfDifferent(nameof(@guid), value);
        }

        public long @fileId
        {
            get => Tiny.GetProperty<long>(nameof(@fileId));
            set => Tiny.AssignIfDifferent(nameof(@fileId), value);
        }

        public int @type
        {
            get => Tiny.GetProperty<int>(nameof(@type));
            set => Tiny.AssignIfDifferent(nameof(@type), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAssetReferenceSpriteAtlas other)
        {
            @guid = other.@guid;
            @fileId = other.@fileId;
            @type = other.@type;
        }
    }
}
