// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Animation
{
    internal partial struct TinyAnimationClip : ITinyComponent
    {
        [TinyCachable]
        private static void RegisterCache(ICacheManagerInternal cacheManager)
        {
            cacheManager.RegisterId<TinyAnimationClip>(s_Id);
            cacheManager.RegisterComponentConstructor(Construct);
        }

        private static TinyAnimationClip Construct(TinyObject tiny) => new TinyAnimationClip(tiny);
        private static TinyId s_Id = CoreIds.Animation.AnimationClip;
        private static TinyType.Reference s_Ref = TypeRefs.Animation.AnimationClip;

        public TinyId ComponentId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyAnimationClip(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, ComponentId);
        }
        public TinyAnimationClip(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public TinyList @animationPlayerDescFloat
        {
            get => Tiny[nameof(@animationPlayerDescFloat)] as TinyList;
        }

        public TinyList @animationPlayerDescVector2
        {
            get => Tiny[nameof(@animationPlayerDescVector2)] as TinyList;
        }

        public TinyList @animationPlayerDescVector3
        {
            get => Tiny[nameof(@animationPlayerDescVector3)] as TinyList;
        }

        public TinyList @animationPlayerDescQuaternion
        {
            get => Tiny[nameof(@animationPlayerDescQuaternion)] as TinyList;
        }

        public TinyList @animationPlayerDescColor
        {
            get => Tiny[nameof(@animationPlayerDescColor)] as TinyList;
        }

        #endregion // Properties

        public void CopyFrom(TinyAnimationClip other)
        {
            CopyList(@animationPlayerDescFloat, other.@animationPlayerDescFloat);
            CopyList(@animationPlayerDescVector2, other.@animationPlayerDescVector2);
            CopyList(@animationPlayerDescVector3, other.@animationPlayerDescVector3);
            CopyList(@animationPlayerDescQuaternion, other.@animationPlayerDescQuaternion);
            CopyList(@animationPlayerDescColor, other.@animationPlayerDescColor);
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
