// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.HitBox2D
{
    internal partial struct TinySprite2DRendererHitBox2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DRendererHitBox2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DRendererHitBox2D Construct(TinyObject tiny) => new TinySprite2DRendererHitBox2D(tiny);
        private static TinyId s_Id = CoreIds.HitBox2D.Sprite2DRendererHitBox2D;
        private static TinyType.Reference s_Ref = TypeRefs.HitBox2D.Sprite2DRendererHitBox2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DRendererHitBox2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DRendererHitBox2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public bool @pixelAccurate
        {
            get => Tiny.GetProperty<bool>(nameof(@pixelAccurate));
            set => Tiny.AssignIfDifferent(nameof(@pixelAccurate), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DRendererHitBox2D other)
        {
            @pixelAccurate = other.@pixelAccurate;
        }
    }
}
