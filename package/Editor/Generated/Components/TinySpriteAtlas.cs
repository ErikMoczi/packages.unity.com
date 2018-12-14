// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySpriteAtlas : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySpriteAtlas>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySpriteAtlas Construct(TinyObject tiny) => new TinySpriteAtlas(tiny);
        private static TinyId s_Id = CoreIds.Core2D.SpriteAtlas;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.SpriteAtlas;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySpriteAtlas(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySpriteAtlas(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @sprites
        {
            get => Tiny[nameof(@sprites)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinySpriteAtlas other)
        {
            CopyList(@sprites, other.@sprites);
        }
        private void CopyList(TinyList lhs, TinyList rhs)
        {
            lhs.Clear();
            foreach (var item in rhs)
            {
                lhs.Add(item);
            }
        }
    }
}
