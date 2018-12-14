// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Interpolation
{
    internal partial struct TinyLinearCurveColor : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyLinearCurveColor>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyLinearCurveColor Construct(TinyObject tiny) => new TinyLinearCurveColor(tiny);
        private static TinyId s_Id = CoreIds.Interpolation.LinearCurveColor;
        private static TinyType.Reference s_Ref = TypeRefs.Interpolation.LinearCurveColor;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyLinearCurveColor(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyLinearCurveColor(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @times
        {
            get => Tiny[nameof(@times)] as TinyList;
        }

        public TinyList @values
        {
            get => Tiny[nameof(@values)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyLinearCurveColor other)
        {
            CopyList(@times, other.@times);
            CopyList(@values, other.@values);
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
