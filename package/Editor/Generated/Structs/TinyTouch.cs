// TINY GENERATED CODE, DO NOT EDIT BY HAND


namespace Unity.Tiny.Runtime.Core2D
{
    internal partial struct TinyTouch : ITinyStruct
    {
        private static TinyId s_Id = CoreIds.Core2D.Touch;
        private static TinyType.Reference s_Ref = TypeRefs.Core2D.Touch;

        public TinyId StructId => s_Id;
        public TinyType.Reference TypeRef => s_Ref;

        public readonly TinyObject Tiny;

        public TinyTouch(TinyObject tiny)
        {
            Tiny = tiny;
            UnityEngine.Assertions.Assert.IsNotNull(Tiny);
            UnityEngine.Assertions.Assert.AreEqual(tiny.Type.Id, StructId);
        }
        public TinyTouch(IRegistry registry) : this(new TinyObject(registry, s_Ref))
        {
        }

        public bool IsValid => null != Tiny;

        #region Properties
        public int @deltaX
        {
            get => Tiny.GetProperty<int>(nameof(@deltaX));
            set => Tiny.AssignIfDifferent(nameof(@deltaX), value);
        }

        public int @deltaY
        {
            get => Tiny.GetProperty<int>(nameof(@deltaY));
            set => Tiny.AssignIfDifferent(nameof(@deltaY), value);
        }

        public int @fingerId
        {
            get => Tiny.GetProperty<int>(nameof(@fingerId));
            set => Tiny.AssignIfDifferent(nameof(@fingerId), value);
        }

        public Core2D.TinyTouchState @phase
        {
            get => Tiny.GetProperty<Core2D.TinyTouchState>(nameof(@phase));
            set => Tiny.AssignIfDifferent(nameof(@phase), value);
        }

        public int @x
        {
            get => Tiny.GetProperty<int>(nameof(@x));
            set => Tiny.AssignIfDifferent(nameof(@x), value);
        }

        public int @y
        {
            get => Tiny.GetProperty<int>(nameof(@y));
            set => Tiny.AssignIfDifferent(nameof(@y), value);
        }

        #endregion // Properties

        public void CopyFrom(TinyTouch other)
        {
            @deltaX = other.@deltaX;
            @deltaY = other.@deltaY;
            @fingerId = other.@fingerId;
            @phase = other.@phase;
            @x = other.@x;
            @y = other.@y;
        }
    }
}
