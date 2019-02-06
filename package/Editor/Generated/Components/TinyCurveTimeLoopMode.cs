// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Interpolation
{
    internal partial struct TinyCurveTimeLoopMode : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyCurveTimeLoopMode>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyCurveTimeLoopMode Construct(TinyObject tiny) => new TinyCurveTimeLoopMode(tiny);
        private static TinyId s_Id = CoreIds.Interpolation.CurveTimeLoopMode;
        private static TinyType.Reference s_Ref = TypeRefs.Interpolation.CurveTimeLoopMode;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyCurveTimeLoopMode(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyCurveTimeLoopMode(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public Core2D.TinyLoopMode @loopMode
        {
            get => Tiny.GetProperty<Core2D.TinyLoopMode>(nameof(@loopMode));
            set => Tiny.AssignIfDifferent(nameof(@loopMode), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyCurveTimeLoopMode other)
        {
            @loopMode = other.@loopMode;
        }
    }
}
