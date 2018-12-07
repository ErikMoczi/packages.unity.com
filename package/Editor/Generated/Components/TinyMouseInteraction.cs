// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControls
{
    internal partial struct TinyMouseInteraction : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyMouseInteraction>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyMouseInteraction Construct(TinyObject tiny) => new TinyMouseInteraction(tiny);
        private static TinyId s_Id = CoreIds.UIControls.MouseInteraction;
        private static TinyType.Reference s_Ref = TypeRefs.UIControls.MouseInteraction;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyMouseInteraction(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyMouseInteraction(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyMouseInteraction other)
        {
        }
    }
}
