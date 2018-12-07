// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyCamera2DAxisSort : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCamera2DAxisSort>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCamera2DAxisSort Construct(TinyObject tiny) => new TinyCamera2DAxisSort(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Camera2DAxisSort;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Camera2DAxisSort;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCamera2DAxisSort(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCamera2DAxisSort(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @axis
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@axis));
            set => Tiny.AssignIfDifferent(nameof(@axis), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCamera2DAxisSort other)
        {
            @axis = other.@axis;
        }
    }
}
