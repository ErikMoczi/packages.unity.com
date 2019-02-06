using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyEntityGroup : IPropertyContainer
    {
        public static ValueClassProperty<TinyEntityGroup, TinyTypeId> TypeIdProperty { get; private set; }
        public static ValueClassProperty<TinyEntityGroup, string> PersistenceIdProperty { get; private set; }
        public static ValueClassProperty<TinyEntityGroup, string> NameProperty { get; private set; }
        public static ValueClassProperty<TinyEntityGroup, int> SerializedVersionProperty { get; private set; }

        private static ClassPropertyBag<TinyEntityGroup> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            PersistenceIdProperty = new ValueClassProperty<TinyEntityGroup, string>(
                "PersistenceId"
                ,c => c.m_PersistenceId
                ,(c, v) => c.m_PersistenceId = v
            );

            NameProperty = new ValueClassProperty<TinyEntityGroup, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            SerializedVersionProperty = new ValueClassProperty<TinyEntityGroup, int>(
                "SerializedVersion"
                ,c => c.m_SerializedVersion
                ,(c, v) => c.m_SerializedVersion = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyEntityGroup>(
                TypeIdProperty,
                PersistenceIdProperty,
                IdProperty,
                NameProperty,
                ExportFlagsProperty,
                SerializedVersionProperty,
                DocumentationProperty,
                EntitiesProperty,
                PrefabInstancesProperty
            );
        }

        static TinyEntityGroup()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_PersistenceId;
        private string m_Name;
        private int m_SerializedVersion;

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


        public partial struct Reference : IStructPropertyContainer<Reference>
        {
            public static ValueStructProperty<Reference, TinyId> IdProperty { get; private set; }
            public static ValueStructProperty<Reference, string> NameProperty { get; private set; }

            private static StructPropertyBag<Reference> s_PropertyBag { get; set; }

            public IPropertyBag PropertyBag => s_PropertyBag;
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

            /// <summary>
            /// Implement this partial method to initialize custom properties
            /// </summary>
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


            public void MakeRef<TContext>(ByRef<Reference, TContext> byRef, TContext context)
            {
                byRef(ref this, context);
            }

            public TReturn MakeRef<TContext, TReturn>(ByRef<Reference, TContext, TReturn> byRef, TContext context)
            {
                return byRef(ref this, context);
            }
        }    }
}
