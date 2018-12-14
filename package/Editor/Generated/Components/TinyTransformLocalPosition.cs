// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTransformLocalPosition : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTransformLocalPosition>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTransformLocalPosition Construct(TinyObject tiny) => new TinyTransformLocalPosition(tiny);
        private static TinyId s_Id = CoreIds.Core2D.TransformLocalPosition;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.TransformLocalPosition;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransformLocalPosition(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTransformLocalPosition(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector3 @position
        {
            get => Tiny.GetProperty<UnityEngine.Vector3>(nameof(@position));
            set => Tiny.AssignIfDifferent(nameof(@position), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTransformLocalPosition other)
        {
            @position = other.@position;
        }
    }
}
