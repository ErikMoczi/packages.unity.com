// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyAssetReferenceAudioClip : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAssetReferenceAudioClip>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAssetReferenceAudioClip Construct(TinyObject tiny) => new TinyAssetReferenceAudioClip(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.AssetReferenceAudioClip;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.AssetReferenceAudioClip;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAssetReferenceAudioClip(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAssetReferenceAudioClip(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyAssetReferenceAudioClip other)
        {
            @guid = other.@guid;
            @fileId = other.@fileId;
            @type = other.@type;
        }
    }
}
