// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTransformNode : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTransformNode>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTransformNode Construct(TinyObject tiny) => new TinyTransformNode(tiny);
        private static TinyId s_Id = CoreIds.Core2D.TransformNode;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.TransformNode;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTransformNode(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTransformNode(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @parent
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@parent));
            set => Tiny.AssignIfDifferent(nameof(@parent), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTransformNode other)
        {
            @parent = other.@parent;
        }
    }
}
