// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Interpolation
{
    internal partial struct TinyBezierCurveVector2 : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyBezierCurveVector2>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyBezierCurveVector2 Construct(TinyObject tiny) => new TinyBezierCurveVector2(tiny);
        private static TinyId s_Id = CoreIds.Interpolation.BezierCurveVector2;
        private static TinyType.Reference s_Ref = TypeRefs.Interpolation.BezierCurveVector2;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyBezierCurveVector2(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyBezierCurveVector2(IRegistry registry) : this(new TinyObject(registry, s_Ref))
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

        public void CopyFrom(TinyBezierCurveVector2 other)
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
