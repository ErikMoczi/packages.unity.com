// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2DBorder : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DBorder>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DBorder Construct(TinyObject tiny) => new TinySprite2DBorder(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2DBorder;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2DBorder;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DBorder(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DBorder(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @bottomLeft
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@bottomLeft));
            set => Tiny.AssignIfDifferent(nameof(@bottomLeft), value);
        }

        public UnityEngine.Vector2 @topRight
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@topRight));
            set => Tiny.AssignIfDifferent(nameof(@topRight), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DBorder other)
        {
            @bottomLeft = other.@bottomLeft;
            @topRight = other.@topRight;
        }
    }
}
