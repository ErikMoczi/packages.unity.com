// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySortingGroup : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySortingGroup>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySortingGroup Construct(TinyObject tiny) => new TinySortingGroup(tiny);
        private static TinyId s_Id = CoreIds.Core2D.SortingGroup;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.SortingGroup;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySortingGroup(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySortingGroup(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinySortingGroup other)
        {
        }
    }
}
