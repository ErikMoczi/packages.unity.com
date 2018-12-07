// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTransformLocalScale : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTransformLocalScale>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTransformLocalScale Construct(TinyObject tiny) => new TinyTransformLocalScale(tiny);
        private static TinyId s_Id = CoreIds.Core2D.TransformLocalScale;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.TransformLocalScale;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransformLocalScale(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTransformLocalScale(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @scale
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@scale));
            set => Tiny.AssignIfDifferent(nameof(@scale), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTransformLocalScale other)
        {
            @scale = other.@scale;
        }
    }
}
