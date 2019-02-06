// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyAssetReferenceTileBase : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAssetReferenceTileBase>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAssetReferenceTileBase Construct(TinyObject tiny) => new TinyAssetReferenceTileBase(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.AssetReferenceTileBase;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.AssetReferenceTileBase;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAssetReferenceTileBase(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAssetReferenceTileBase(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyAssetReferenceTileBase other)
        {
            @guid = other.@guid;
            @fileId = other.@fileId;
            @type = other.@type;
        }
    }
}
