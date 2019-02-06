// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationClipSource : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAnimationClipSource>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAnimationClipSource Construct(TinyObject tiny) => new TinyAnimationClipSource(tiny);
        private static TinyId s_Id = CoreIds.Animation.AnimationClipSource;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationClipSource;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationClipSource(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAnimationClipSource(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public string @file
        {
            get => Tiny.GetProperty<string>(nameof(@file));
            set => Tiny.AssignIfDifferent(nameof(@file), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationClipSource other)
        {
            @file = other.@file;
        }
    }
}
