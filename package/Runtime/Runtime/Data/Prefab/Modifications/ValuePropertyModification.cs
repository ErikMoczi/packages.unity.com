using JetBrains.Annotations;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// Property modification for a component value
    /// </summary>
    /// <typeparam name="TValue"></typeparam>
    internal struct ValuePropertyModification<TValue> : IStructPropertyContainer<ValuePropertyModification<TValue>>, IPropertyModification
    {
        private static class Properties
        {
            public static readonly StructValueStructProperty<ValuePropertyModification<TValue>, TinyType.Reference> Target =
                new StructValueStructProperty<ValuePropertyModification<TValue>, TinyType.Reference>(
                    nameof(Target),
                    (ref ValuePropertyModification<TValue> c) => c.m_Target,
                    (ref ValuePropertyModification<TValue> c, TinyType.Reference v) => c.m_Target = v,
                    (StructValueStructProperty<ValuePropertyModification<TValue>, TinyType.Reference>.ByRef m,
                        StructValueStructProperty<ValuePropertyModification<TValue>, TinyType.Reference> p,
                        ref ValuePropertyModification<TValue> c,
                        IPropertyVisitor v) => m(p, ref c, ref c.m_Target, v)
                );

            public static readonly ValueStructProperty<ValuePropertyModification<TValue>, string> Path =
                new ValueStructProperty<ValuePropertyModification<TValue>, string>(
                    nameof(Path),
                    (ref ValuePropertyModification<TValue> c) => c.m_Path,
                    (ref ValuePropertyModification<TValue> c, string v) =>
                    {
                        c.m_Path = v;
                        c.m_ExpandedPath = null;
                    });

            public static readonly ValueStructProperty<ValuePropertyModification<TValue>, int> TypeCode =
                new ValueStructProperty<ValuePropertyModification<TValue>, int>(
                    nameof(TypeCode),
                    (ref ValuePropertyModification<TValue> c) => PropertyModificationConverter.GetSerializedTypeId(typeof(TValue)),
                    null
                );

            public static readonly ValueStructProperty<ValuePropertyModification<TValue>, TValue> Value =
                new ValueStructProperty<ValuePropertyModification<TValue>, TValue>(
                    nameof(Value),
                    (ref ValuePropertyModification<TValue> c) => c.m_Value,
                    (ref ValuePropertyModification<TValue> c, TValue v) => c.m_Value = v
                );

            public static readonly StructPropertyBag<ValuePropertyModification<TValue>> PropertyBag =
                new StructPropertyBag<ValuePropertyModification<TValue>>(
                    Target,
                    Path,
                    TypeCode,
                    Value
                );
        }

        public IVersionStorage VersionStorage => null;
        public IPropertyBag PropertyBag => Properties.PropertyBag;

        private TinyType.Reference m_Target;
        private string m_Path;
        private TValue m_Value;
        
        /// <summary>
        /// Cached expanded path
        /// </summary>
        private PropertyPath m_ExpandedPath;

        [UsedImplicitly]
        public ValuePropertyModification(TinyType.Reference target, string path, TValue value)
        {
            m_Target = target;
            m_Path = path;
            m_Value = value;
            m_ExpandedPath = null;
        }

        public TinyType.Reference Target
        {
            get => Properties.Target.GetValue(ref this);
            set => Properties.Target.SetValue(ref this, value);
        }

        public string Path
        {
            get => Properties.Path.GetValue(ref this);
            set => Properties.Path.SetValue(ref this, value);
        }
        
        public int TypeCode => Properties.TypeCode.GetValue(ref this);
        
        public TValue Value
        {
            get => Properties.Value.GetValue(ref this);
            set => Properties.Value.SetValue(ref this, value);
        }

        object IPropertyModification.Value
        {
            get => Properties.Value.GetObjectValue(ref this);
            set => Properties.Value.SetObjectValue(ref this, value);
        }

        public PropertyPath GetFullPath()
        {
            return m_ExpandedPath ?? (m_ExpandedPath = PrefabManager.ExpandPropertyPath(m_Path));
        }

        public void MakeRef<TContext>(ByRef<ValuePropertyModification<TValue>, TContext> byRef, TContext context)
        {
            byRef(ref this, context);
        }

        public TReturn MakeRef<TContext, TReturn>(ByRef<ValuePropertyModification<TValue>, TContext, TReturn> byRef, TContext context)
        {
            return byRef(ref this, context);
        }
    }
}