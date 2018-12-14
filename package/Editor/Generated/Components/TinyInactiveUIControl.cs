// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UIControls
{
    internal partial struct TinyInactiveUIControl : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyInactiveUIControl>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyInactiveUIControl Construct(TinyObject tiny) => new TinyInactiveUIControl(tiny);
        private static TinyId s_Id = CoreIds.UIControls.InactiveUIControl;
        private static TinyType.Reference s_Ref = TypeRefs.UIControls.InactiveUIControl;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyInactiveUIControl(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyInactiveUIControl(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;


        public void CopyFrom(TinyInactiveUIControl other)
        {
        }
    }
}
