// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Tweens
{
    internal partial struct TinyTweenQuaternion : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyTweenQuaternion>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyTweenQuaternion Construct(TinyObject tiny) => new TinyTweenQuaternion(tiny);
        private static TinyId s_Id = CoreIds.Tweens.TweenQuaternion;
        private static TinyType.Reference s_Ref = TypeRefs.Tweens.TweenQuaternion;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTweenQuaternion(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyTweenQuaternion(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public UnityEngine.Quaternion @start
        {
            get => Tiny.GetProperty<UnityEngine.Quaternion>(nameof(@start));
            set => Tiny.AssignIfDifferent(nameof(@start), value);
        }

        public UnityEngine.Quaternion @end
        {
            get => Tiny.GetProperty<UnityEngine.Quaternion>(nameof(@end));
            set => Tiny.AssignIfDifferent(nameof(@end), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTweenQuaternion other)
        {
            @start = other.@start;
            @end = other.@end;
        }
    }
}
