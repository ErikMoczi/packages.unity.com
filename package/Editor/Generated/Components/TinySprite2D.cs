// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2D Construct(TinyObject tiny) => new TinySprite2D(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2D;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Texture2D @image
        {
            get => Tiny.GetProperty<UnityEngine.Texture2D>(nameof(@image));
            set => Tiny.AssignIfDifferent(nameof(@image), value);
        }

        public UnityEngine.Rect @imageRegion
        {
            get => Tiny.GetProperty<UnityEngine.Rect>(nameof(@imageRegion));
            set => Tiny.AssignIfDifferent(nameof(@imageRegion), value);
        }

        public UnityEngine.Vector2 @pivot
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@pivot));
            set => Tiny.AssignIfDifferent(nameof(@pivot), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2D other)
        {
            @image = other.@image;
            @imageRegion = other.@imageRegion;
            @pivot = other.@pivot;
        }
    }
}
