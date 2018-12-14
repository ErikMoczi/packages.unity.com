// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyCameraCullingMask : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCameraCullingMask>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCameraCullingMask Construct(TinyObject tiny) => new TinyCameraCullingMask(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.CameraCullingMask;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.CameraCullingMask;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCameraCullingMask(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCameraCullingMask(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @mask
        {
            get => Tiny.GetProperty<int>(nameof(@mask));
            set => Tiny.AssignIfDifferent(nameof(@mask), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCameraCullingMask other)
        {
            @mask = other.@mask;
        }
    }
}
