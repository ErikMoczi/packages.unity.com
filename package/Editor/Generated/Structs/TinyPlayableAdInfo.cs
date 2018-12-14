// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.PlayableAd
{
    internal partial struct TinyPlayableAdInfo : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.PlayableAd.PlayableAdInfo;
        private static TinyType.Reference s_Ref = TypeRefs.PlayableAd.PlayableAdInfo;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyPlayableAdInfo(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyPlayableAdInfo(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @googlePlayStoreUrl
        {
            get => Tiny.GetProperty<string>(nameof(@googlePlayStoreUrl));
            set => Tiny.AssignIfDifferent(nameof(@googlePlayStoreUrl), value);
        }

        public string @appStoreUrl
        {
            get => Tiny.GetProperty<string>(nameof(@appStoreUrl));
            set => Tiny.AssignIfDifferent(nameof(@appStoreUrl), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyPlayableAdInfo other)
        {
            @googlePlayStoreUrl = other.@googlePlayStoreUrl;
            @appStoreUrl = other.@appStoreUrl;
        }
    }
}
