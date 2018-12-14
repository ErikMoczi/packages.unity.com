// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Text
{
    internal partial struct TinyBitmapFont : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyBitmapFont>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyBitmapFont Construct(TinyObject tiny) => new TinyBitmapFont(tiny);
        private static TinyId s_Id = CoreIds.Text.BitmapFont;
        private static TinyType.Reference s_Ref = TypeRefs.Text.BitmapFont;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyBitmapFont(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyBitmapFont(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Unity.Tiny.TinyEntity.Reference @textureAtlas
        {
            get => Tiny.GetProperty<Unity.Tiny.TinyEntity.Reference>(nameof(@textureAtlas));
            set => Tiny.AssignIfDifferent(nameof(@textureAtlas), value);
        }

        public TinyList @data
        {
            get => Tiny[nameof(@data)] as TinyList;
        }

        public float @size
        {
            get => Tiny.GetProperty<float>(nameof(@size));
            set => Tiny.AssignIfDifferent(nameof(@size), value);
        }

        public float @ascent
        {
            get => Tiny.GetProperty<float>(nameof(@ascent));
            set => Tiny.AssignIfDifferent(nameof(@ascent), value);
        }

        public float @descent
        {
            get => Tiny.GetProperty<float>(nameof(@descent));
            set => Tiny.AssignIfDifferent(nameof(@descent), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyBitmapFont other)
        {
            @textureAtlas = other.@textureAtlas;
            CopyList(@data, other.@data);
            @size = other.@size;
            @ascent = other.@ascent;
            @descent = other.@descent;
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
