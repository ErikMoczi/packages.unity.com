
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyType : IPropertyContainer
    {
        public static ValueClassProperty<TinyType, TinyTypeId> TypeIdProperty { get; private set; }
        public static ValueClassProperty<TinyType, string> PersistenceIdProperty { get; private set; }
        public static ValueClassProperty<TinyType, string> NameProperty { get; private set; }
        public static ValueClassProperty<TinyType, int> SerializedVersionProperty { get; private set; }
        public static ValueClassProperty<TinyType, bool> UnlistedProperty { get; private set; }
        public static ValueClassProperty<TinyType, TinyVisibility> VisibilityProperty { get; private set; }
        public static ValueClassProperty<TinyType, TinyTypeCode> TypeCodeProperty { get; private set; }
        public static StructValueClassProperty<TinyType, Reference> BaseTypeProperty { get; private set; }
        public static ClassListClassProperty<TinyType, TinyField> FieldsProperty { get; private set; }

        private static ClassPropertyBag<TinyType> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            PersistenceIdProperty = new ValueClassProperty<TinyType, string>(
                "PersistenceId"
                ,c => c.m_PersistenceId
                ,(c, v) => c.m_PersistenceId = v
            );

            NameProperty = new ValueClassProperty<TinyType, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            SerializedVersionProperty = new ValueClassProperty<TinyType, int>(
                "SerializedVersion"
                ,c => c.m_SerializedVersion
                ,(c, v) => c.m_SerializedVersion = v
            );

            UnlistedProperty = new ValueClassProperty<TinyType, bool>(
                "Unlisted"
                ,c => c.m_Unlisted
                ,(c, v) => c.m_Unlisted = v
            );

            VisibilityProperty = new ValueClassProperty<TinyType, TinyVisibility>(
                "Visibility"
                ,c => c.m_Visibility
                ,(c, v) => c.m_Visibility = v
            );

            TypeCodeProperty = new ValueClassProperty<TinyType, TinyTypeCode>(
                "TypeCode"
                ,c => c.m_TypeCode
                ,(c, v) => c.m_TypeCode = v
            );

            BaseTypeProperty = new StructValueClassProperty<TinyType, Reference>(
                "BaseType"
                ,c => c.m_BaseType
                ,(c, v) => c.m_BaseType = v
                ,(m, p, c, v) => m(p, c, ref c.m_BaseType, v)
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyType>(
                TypeIdProperty,
                PersistenceIdProperty,
                IdProperty,
                NameProperty,
                SerializedVersionProperty,
                ExportFlagsProperty,
                UnlistedProperty,
                VisibilityProperty,
                DocumentationProperty,
                TypeCodeProperty,
                BaseTypeProperty,
                FieldsProperty
            );
        }

        private string m_PersistenceId;
        private string m_Name;
        private int m_SerializedVersion;
        private bool m_Unlisted = false;
        private TinyVisibility m_Visibility = TinyVisibility.Normal;
        private TinyTypeCode m_TypeCode;
        private Reference m_BaseType;
        private readonly List<TinyField> m_Fields = new List<TinyField>();

        public string PersistenceId
        {
            get { return PersistenceIdProperty.GetValue(this); }
            set { PersistenceIdProperty.SetValue(this, value); }
        }

        public int SerializedVersion
        {
            get { return SerializedVersionProperty.GetValue(this); }
            set { SerializedVersionProperty.SetValue(this, value); }
        }

        public bool Unlisted
        {
            get { return UnlistedProperty.GetValue(this); }
            set { UnlistedProperty.SetValue(this, value); }
        }

        public TinyVisibility Visibility
        {
            get { return VisibilityProperty.GetValue(this); }
            set { VisibilityProperty.SetValue(this, value); }
        }

        public TinyTypeCode TypeCode
        {
            get { return TypeCodeProperty.GetValue(this); }
            set { TypeCodeProperty.SetValue(this, value); }
        }

        public Reference BaseType
        {
            get { return BaseTypeProperty.GetValue(this); }
            set { BaseTypeProperty.SetValue(this, value); }
        }

        public PropertyList<TinyType, TinyField> Fields => new PropertyList<TinyType, TinyField>(FieldsProperty, this);


        public partial struct Reference : IStructPropertyContainer<Reference>
        {
            public static ValueStructProperty<Reference, TinyId> IdProperty { get; private set; }
            public static ValueStructProperty<Reference, string> NameProperty { get; private set; }

            private static StructPropertyBag<Reference> s_PropertyBag { get; set; }

            /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
            public IPropertyBag PropertyBag => s_PropertyBag;
            /// <inheritdoc cref="Unity.Properties.IPropertyContainer.VersionStorage" />
            public IVersionStorage VersionStorage => null;

            private static void InitializeProperties()
            {
                IdProperty = new ValueStructProperty<Reference, TinyId>(
                    "Id"
                    ,(ref Reference c) => c.m_Id
                    ,(ref Reference c, TinyId v) => c.m_Id = v
                );

                NameProperty = new ValueStructProperty<Reference, string>(
                    "Name"
                    ,(ref Reference c) => c.m_Name
                    ,(ref Reference c, string v) => c.m_Name = v
                );
            }

            static partial void InitializeCustomProperties();

            private static void InitializePropertyBag()
            {
                s_PropertyBag = new StructPropertyBag<Reference>(
                    IdProperty,
                    NameProperty
                );
            }

            static Reference()
            {
                InitializeProperties();
                InitializeCustomProperties();
                InitializePropertyBag();
            }

            private TinyId m_Id;
            private string m_Name;

            public TinyId Id
            {
                get { return IdProperty.GetValue(ref this); }
                set { IdProperty.SetValue(ref this, value); }
            }

            public string Name
            {
                get { return NameProperty.GetValue(ref this); }
                set { NameProperty.SetValue(ref this, value); }
            }


            /// <summary>
            /// Pass this object as a reference to the given handler.
            /// </summary>
            /// <param name="byRef">Handler to invoke.</param>
            /// <param name="context">Context argument passed to the handler.</param>
            public void MakeRef<TContext>(ByRef<Reference, TContext> byRef, TContext context)
            {
                byRef(ref this, context);
            }

            /// <summary>
            /// Pass this object as a reference to the given handler, and return the result.
            /// </summary>
            /// <param name="byRef">Handler to invoke.</param>
            /// <param name="context">Context argument passed to the handler.</param>
            /// <returns>The handler's return value.</returns>
            public TReturn MakeRef<TContext, TReturn>(ByRef<Reference, TContext, TReturn> byRef, TContext context)
            {
                return byRef(ref this, context);
            }
        }    }
}
