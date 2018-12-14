// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Interpolation
{
    internal partial struct TinyBezierCurveFloat : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyBezierCurveFloat>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyBezierCurveFloat Construct(TinyObject tiny) => new TinyBezierCurveFloat(tiny);
        private static TinyId s_Id = CoreIds.Interpolation.BezierCurveFloat;
        private static TinyType.Reference s_Ref = TypeRefs.Interpolation.BezierCurveFloat;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyBezierCurveFloat(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyBezierCurveFloat(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public TinyList @outValues
        {
            get => Tiny[nameof(@outValues)] as TinyList;
        }

        public TinyList @inValues
        {
            get => Tiny[nameof(@inValues)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyBezierCurveFloat other)
        {
            CopyList(@times, other.@times);
            CopyList(@values, other.@values);
            CopyList(@outValues, other.@outValues);
            CopyList(@inValues, other.@inValues);
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
