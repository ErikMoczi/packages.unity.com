// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyCamera2DClippingPlanes : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCamera2DClippingPlanes>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCamera2DClippingPlanes Construct(TinyObject tiny) => new TinyCamera2DClippingPlanes(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Camera2DClippingPlanes;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Camera2DClippingPlanes;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCamera2DClippingPlanes(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCamera2DClippingPlanes(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @near
        {
            get => Tiny.GetProperty<float>(nameof(@near));
            set => Tiny.AssignIfDifferent(nameof(@near), value);
        }

        public float @far
        {
            get => Tiny.GetProperty<float>(nameof(@far));
            set => Tiny.AssignIfDifferent(nameof(@far), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCamera2DClippingPlanes other)
        {
            @near = other.@near;
            @far = other.@far;
        }
    }
}
