// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyImage2DLoadFromFile : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyImage2DLoadFromFile>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyImage2DLoadFromFile Construct(TinyObject tiny) => new TinyImage2DLoadFromFile(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Image2DLoadFromFile;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Image2DLoadFromFile;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyImage2DLoadFromFile(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyImage2DLoadFromFile(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @imageFile
        {
            get => Tiny.GetProperty<string>(nameof(@imageFile));
            set => Tiny.AssignIfDifferent(nameof(@imageFile), value);
        }

        public string @maskFile
        {
            get => Tiny.GetProperty<string>(nameof(@maskFile));
            set => Tiny.AssignIfDifferent(nameof(@maskFile), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyImage2DLoadFromFile other)
        {
            @imageFile = other.@imageFile;
            @maskFile = other.@maskFile;
        }
    }
}
