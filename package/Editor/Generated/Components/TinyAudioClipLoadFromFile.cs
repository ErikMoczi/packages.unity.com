// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Audio
{
    internal partial struct TinyAudioClipLoadFromFile : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAudioClipLoadFromFile>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAudioClipLoadFromFile Construct(TinyObject tiny) => new TinyAudioClipLoadFromFile(tiny);
        private static TinyId s_Id = CoreIds.Audio.AudioClipLoadFromFile;
        private static TinyType.Reference s_Ref = TypeRefs.Audio.AudioClipLoadFromFile;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAudioClipLoadFromFile(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAudioClipLoadFromFile(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @fileName
        {
            get => Tiny.GetProperty<string>(nameof(@fileName));
            set => Tiny.AssignIfDifferent(nameof(@fileName), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAudioClipLoadFromFile other)
        {
            @fileName = other.@fileName;
        }
    }
}
