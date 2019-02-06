using JetBrains.Annotations;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// Property modification for a component value
    /// </summary>
    internal struct ContainerPropertyModification : IStructPropertyContainer<ContainerPropertyModification>, IPropertyModification
    {
        private static class Properties
        {
            public static readonly StructValueStructProperty<ContainerPropertyModification, TinyType.Reference> Target =
                new StructValueStructProperty<ContainerPropertyModification, TinyType.Reference>(
                    nameof(Target),
                    (ref ContainerPropertyModification c) => c.m_Target,
                    (ref ContainerPropertyModification c, TinyType.Reference v) => c.m_Target = v,
                    (StructValueStructProperty<ContainerPropertyModification, TinyType.Reference>.ByRef m,
                        StructValueStructProperty<ContainerPropertyModification, TinyType.Reference> p,
                        ref ContainerPropertyModification c,
                        IPropertyVisitor v) => m(p, ref c, ref c.m_Target, v)
                );

            public static readonly ValueStructProperty<ContainerPropertyModification, string> Path =
                new ValueStructProperty<ContainerPropertyModification, string>(
                    nameof(Path),
                    (ref ContainerPropertyModification c) => c.m_Path,
                    (ref ContainerPropertyModification c, string v) => c.m_Path = v
                );

            public static readonly ValueStructProperty<ContainerPropertyModification, int> TypeCode =
                new ValueStructProperty<ContainerPropertyModification, int>(
                    nameof(TypeCode),
                    (ref ContainerPropertyModification c) => PropertyModificationConverter.GetSerializedTypeId(c.m_Value.GetType()),
                    null
                );

            public static readonly ClassValueStructProperty<ContainerPropertyModification, IPropertyContainer> Value =
                new ClassValueStructProperty<ContainerPropertyModification, IPropertyContainer>(
                    nameof(Value),
                    (ref ContainerPropertyModification c) => c.m_Value,
                    (ref ContainerPropertyModification c, IPropertyContainer v) => c.m_Value = v
                );

            public static readonly StructPropertyBag<ContainerPropertyModification> PropertyBag =
                new StructPropertyBag<ContainerPropertyModification>(
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
        private IPropertyContainer m_Value;
        
        /// <summary>
        /// Cached expanded path
        /// </summary>
        private PropertyPath m_ExpandedPath;

        [UsedImplicitly]
        public ContainerPropertyModification(TinyType.Reference target, string path, IPropertyContainer value)
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

        public IPropertyContainer Value
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

        public void MakeRef<TContext>(ByRef<ContainerPropertyModification, TContext> byRef, TContext context)
        {
            byRef(ref this, context);
        }

        public TReturn MakeRef<TContext, TReturn>(ByRef<ContainerPropertyModification, TContext, TReturn> byRef, TContext context)
        {
            return byRef(ref this, context);
        }
    }
}