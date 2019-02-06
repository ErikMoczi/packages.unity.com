// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyLayerSorting : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyLayerSorting>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyLayerSorting Construct(TinyObject tiny) => new TinyLayerSorting(tiny);
        private static TinyId s_Id = CoreIds.Core2D.LayerSorting;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.LayerSorting;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyLayerSorting(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyLayerSorting(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @layer
        {
            get => Tiny.GetProperty<int>(nameof(@layer));
            set => Tiny.AssignIfDifferent(nameof(@layer), value);
        }

        public int @order
        {
            get => Tiny.GetProperty<int>(nameof(@order));
            set => Tiny.AssignIfDifferent(nameof(@order), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyLayerSorting other)
        {
            @layer = other.@layer;
            @order = other.@order;
        }
    }
}
