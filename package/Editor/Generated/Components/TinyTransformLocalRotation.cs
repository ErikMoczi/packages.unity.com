// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTransformLocalRotation : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTransformLocalRotation>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTransformLocalRotation Construct(TinyObject tiny) => new TinyTransformLocalRotation(tiny);
        private static TinyId s_Id = CoreIds.Core2D.TransformLocalRotation;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.TransformLocalRotation;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransformLocalRotation(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTransformLocalRotation(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Quaternion @rotation
        {
            get => Tiny.GetProperty<UnityEngine.Quaternion>(nameof(@rotation));
            set => Tiny.AssignIfDifferent(nameof(@rotation), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTransformLocalRotation other)
        {
            @rotation = other.@rotation;
        }
    }
}
