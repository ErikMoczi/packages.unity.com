// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyImage2DLoadFromMemory : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyImage2DLoadFromMemory>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyImage2DLoadFromMemory Construct(TinyObject tiny) => new TinyImage2DLoadFromMemory(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Image2DLoadFromMemory;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Image2DLoadFromMemory;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyImage2DLoadFromMemory(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyImage2DLoadFromMemory(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @width
        {
            get => Tiny.GetProperty<int>(nameof(@width));
            set => Tiny.AssignIfDifferent(nameof(@width), value);
        }

        public int @height
        {
            get => Tiny.GetProperty<int>(nameof(@height));
            set => Tiny.AssignIfDifferent(nameof(@height), value);
        }

        public TinyList @pixelData
        {
            get => Tiny[nameof(@pixelData)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyImage2DLoadFromMemory other)
        {
            @width = other.@width;
            @height = other.@height;
            CopyList(@pixelData, other.@pixelData);
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
