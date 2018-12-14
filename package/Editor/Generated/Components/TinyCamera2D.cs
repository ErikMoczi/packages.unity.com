// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyCamera2D : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCamera2D>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCamera2D Construct(TinyObject tiny) => new TinyCamera2D(tiny);
        private static TinyId s_Id = CoreIds.Core2D.Camera2D;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Camera2D;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCamera2D(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCamera2D(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public float @halfVerticalSize
        {
            get => Tiny.GetProperty<float>(nameof(@halfVerticalSize));
            set => Tiny.AssignIfDifferent(nameof(@halfVerticalSize), value);
        }

        public UnityEngine.Rect @rect
        {
            get => Tiny.GetProperty<UnityEngine.Rect>(nameof(@rect));
            set => Tiny.AssignIfDifferent(nameof(@rect), value);
        }

        public UnityEngine.Color @backgroundColor
        {
            get => Tiny.GetProperty<UnityEngine.Color>(nameof(@backgroundColor));
            set => Tiny.AssignIfDifferent(nameof(@backgroundColor), value);
        }

        public UnityEngine.CameraClearFlags @clearFlags
        {
            get => Tiny.GetProperty<UnityEngine.CameraClearFlags>(nameof(@clearFlags));
            set => Tiny.AssignIfDifferent(nameof(@clearFlags), value);
        }

        public float @depth
        {
            get => Tiny.GetProperty<float>(nameof(@depth));
            set => Tiny.AssignIfDifferent(nameof(@depth), value);
        }

        public TinyList @cullingMask
        {
            get => Tiny[nameof(@cullingMask)] as TinyList;
        }

        public int @layerMask
        {
            get => Tiny.GetProperty<int>(nameof(@layerMask));
            set => Tiny.AssignIfDifferent(nameof(@layerMask), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCamera2D other)
        {
            @halfVerticalSize = other.@halfVerticalSize;
            @rect = other.@rect;
            @backgroundColor = other.@backgroundColor;
            @clearFlags = other.@clearFlags;
            @depth = other.@depth;
            CopyList(@cullingMask, other.@cullingMask);
            @layerMask = other.@layerMask;
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
