// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyAssetReferenceAnimationClip : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAssetReferenceAnimationClip>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAssetReferenceAnimationClip Construct(TinyObject tiny) => new TinyAssetReferenceAnimationClip(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.AssetReferenceAnimationClip;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.AssetReferenceAnimationClip;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAssetReferenceAnimationClip(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAssetReferenceAnimationClip(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyAssetReferenceAnimationClip other)
        {
            @guid = other.@guid;
            @fileId = other.@fileId;
            @type = other.@type;
        }
    }
}
