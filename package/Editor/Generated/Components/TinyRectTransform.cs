// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.UILayout
{
    internal partial struct TinyRectTransform : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyRectTransform>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyRectTransform Construct(TinyObject tiny) => new TinyRectTransform(tiny);
        private static TinyId s_Id = CoreIds.UILayout.RectTransform;
        private static TinyType.Reference s_Ref = TypeRefs.UILayout.RectTransform;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyRectTransform(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyRectTransform(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Vector2 @anchorMin
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@anchorMin));
            set => Tiny.AssignIfDifferent(nameof(@anchorMin), value);
        }

        public UnityEngine.Vector2 @anchorMax
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@anchorMax));
            set => Tiny.AssignIfDifferent(nameof(@anchorMax), value);
        }

        public UnityEngine.Vector2 @sizeDelta
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@sizeDelta));
            set => Tiny.AssignIfDifferent(nameof(@sizeDelta), value);
        }

        public UnityEngine.Vector2 @anchoredPosition
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@anchoredPosition));
            set => Tiny.AssignIfDifferent(nameof(@anchoredPosition), value);
        }

        public UnityEngine.Vector2 @pivot
        {
            get => Tiny.GetProperty<UnityEngine.Vector2>(nameof(@pivot));
            set => Tiny.AssignIfDifferent(nameof(@pivot), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyRectTransform other)
        {
            @anchorMin = other.@anchorMin;
            @anchorMax = other.@anchorMax;
            @sizeDelta = other.@sizeDelta;
            @anchoredPosition = other.@anchoredPosition;
            @pivot = other.@pivot;
        }
    }
}
