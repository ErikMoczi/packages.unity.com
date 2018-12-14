// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinySprite2DSequence : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinySprite2DSequence>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinySprite2DSequence Construct(TinyObject tiny) => new TinySprite2DSequence(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Sprite2DSequence;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Sprite2DSequence;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinySprite2DSequence(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinySprite2DSequence(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @sprites
        {
            get => Tiny[nameof(@sprites)] as TinyList;
        }

        public float @frameRate
        {
            get => Tiny.GetProperty<float>(nameof(@frameRate));
            set => Tiny.AssignIfDifferent(nameof(@frameRate), value);
        }

        #endregion // Properties

        public void CopyFrom(TinySprite2DSequence other)
        {
            CopyList(@sprites, other.@sprites);
            @frameRate = other.@frameRate;
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
