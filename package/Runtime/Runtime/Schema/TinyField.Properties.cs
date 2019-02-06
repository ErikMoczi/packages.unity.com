
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyField : IPropertyContainer
    {
        public static ValueClassProperty<TinyField, TinyId> IdProperty { get; private set; }
        public static ValueClassProperty<TinyField, string> NameProperty { get; private set; }
        public static ClassValueClassProperty<TinyField, TinyDocumentation> DocumentationProperty { get; private set; }
        public static ValueClassProperty<TinyField, TinyType.Reference> FieldTypeProperty { get; private set; }
        public static ValueClassProperty<TinyField, bool> ArrayProperty { get; private set; }
        public static ValueClassProperty<TinyField, TinyVisibility> VisibilityProperty { get; private set; }
        public static ValueClassProperty<TinyField, bool> EditorOnlyProperty { get; private set; }
        public static ValueClassProperty<TinyField, bool> VersionedProperty { get; private set; }

        private static ClassPropertyBag<TinyField> s_PropertyBag { get; set; }

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;

        private static void InitializeProperties()
        {
            IdProperty = new ValueClassProperty<TinyField, TinyId>(
                "Id"
                ,c => c.m_Id
                ,(c, v) => c.m_Id = v
            );

            NameProperty = new ValueClassProperty<TinyField, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            FieldTypeProperty = new ValueClassProperty<TinyField, TinyType.Reference>(
                "FieldType"
                ,c => c.m_FieldType
                ,(c, v) => c.m_FieldType = v
            );

            ArrayProperty = new ValueClassProperty<TinyField, bool>(
                "Array"
                ,c => c.m_Array
                ,(c, v) => c.m_Array = v
            );

            VisibilityProperty = new ValueClassProperty<TinyField, TinyVisibility>(
                "Visibility"
                ,c => c.m_Visibility
                ,(c, v) => c.m_Visibility = v
            );

            EditorOnlyProperty = new ValueClassProperty<TinyField, bool>(
                "EditorOnly"
                ,c => c.m_EditorOnly
                ,(c, v) => c.m_EditorOnly = v
            );

            VersionedProperty = new ValueClassProperty<TinyField, bool>(
                "Versioned"
                ,c => c.m_Versioned
                ,(c, v) => c.m_Versioned = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyField>(
                IdProperty,
                NameProperty,
                DocumentationProperty,
                FieldTypeProperty,
                ArrayProperty,
                VisibilityProperty,
                EditorOnlyProperty,
                VersionedProperty
            );
        }

        static TinyField()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private TinyId m_Id;
        private string m_Name;
        private TinyDocumentation m_Documentation;
        private TinyType.Reference m_FieldType;
        private bool m_Array;
        private TinyVisibility m_Visibility;
        private bool m_EditorOnly;
        private bool m_Versioned = true;

        public TinyId Id
        {
            get { return IdProperty.GetValue(this); }
            set { IdProperty.SetValue(this, value); }
        }

        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }

        public TinyDocumentation Documentation
        {
            get { return DocumentationProperty.GetValue(this); }
            set { DocumentationProperty.SetValue(this, value); }
        }

        public TinyType.Reference FieldType
        {
            get { return FieldTypeProperty.GetValue(this); }
            set { FieldTypeProperty.SetValue(this, value); }
        }

        public bool Array
        {
            get { return ArrayProperty.GetValue(this); }
            set { ArrayProperty.SetValue(this, value); }
        }

        public TinyVisibility Visibility
        {
            get { return VisibilityProperty.GetValue(this); }
            set { VisibilityProperty.SetValue(this, value); }
        }

        public bool EditorOnly
        {
            get { return EditorOnlyProperty.GetValue(this); }
            set { EditorOnlyProperty.SetValue(this, value); }
        }

        public bool Versioned
        {
            get { return VersionedProperty.GetValue(this); }
            set { VersionedProperty.SetValue(this, value); }
        }


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
