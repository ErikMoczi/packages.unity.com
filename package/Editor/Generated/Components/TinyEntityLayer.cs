// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.EditorExtensions
{
    internal partial struct TinyEntityLayer : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyEntityLayer>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyEntityLayer Construct(TinyObject tiny) => new TinyEntityLayer(tiny);
        private static TinyId s_Id = CoreIds.EditorExtensions.EntityLayer;
        private static TinyType.Reference s_Ref = TypeRefs.EditorExtensions.EntityLayer;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyEntityLayer(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyEntityLayer(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @layer
        {
            get => Tiny.GetProperty<int>(nameof(@layer));
            set => Tiny.AssignIfDifferent(nameof(@layer), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyEntityLayer other)
        {
            @layer = other.@layer;
        }
    }
}
