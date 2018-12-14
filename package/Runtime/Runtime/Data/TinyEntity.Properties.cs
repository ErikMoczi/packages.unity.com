using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyEntity : IPropertyContainer
    {
        public static ValueClassProperty<TinyEntity, TinyTypeId> TypeIdProperty { get; private set; }
        public static ValueClassProperty<TinyEntity, string> NameProperty { get; private set; }
        public static ValueClassProperty<TinyEntity, bool> EnabledProperty { get; private set; }
        public static ValueClassProperty<TinyEntity, bool> StaticProperty { get; private set; }
        public static ValueClassProperty<TinyEntity, int> LayerProperty { get; private set; }
        public static StructValueClassProperty<TinyEntity, TinyEntityGroup.Reference> EntityGroupProperty { get; private set; }
        public static ClassValueClassProperty<TinyEntity, TinyEntityInstance> InstanceProperty { get; private set; }

        private static ClassPropertyBag<TinyEntity> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            NameProperty = new ValueClassProperty<TinyEntity, string>(
                "Name"
                ,c => c.m_Name
                ,(c, v) => c.m_Name = v
            );

            EnabledProperty = new ValueClassProperty<TinyEntity, bool>(
                "Enabled"
                ,c => c.m_Enabled
                ,(c, v) => c.m_Enabled = v
            );

            StaticProperty = new ValueClassProperty<TinyEntity, bool>(
                "Static"
                ,c => c.m_Static
                ,(c, v) => c.m_Static = v
            );

            LayerProperty = new ValueClassProperty<TinyEntity, int>(
                "Layer"
                ,c => c.m_Layer
                ,(c, v) => c.m_Layer = v
            );

            EntityGroupProperty = new StructValueClassProperty<TinyEntity, TinyEntityGroup.Reference>(
                "EntityGroup"
                ,c => c.m_EntityGroup
                ,(c, v) => c.m_EntityGroup = v
                ,(m, p, c, v) => m(p, c, ref c.m_EntityGroup, v)
            );

            InstanceProperty = new ClassValueClassProperty<TinyEntity, TinyEntityInstance>(
                "Instance"
                ,c => c.m_Instance
                ,(c, v) => c.m_Instance = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyEntity>(
                TypeIdProperty,
                IdProperty,
                NameProperty,
                EnabledProperty,
                StaticProperty,
                LayerProperty,
                ComponentsProperty,
                EntityGroupProperty,
                InstanceProperty
            );
        }

        static TinyEntity()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Name;
        private bool m_Enabled = true;
        private bool m_Static = false;
        private int m_Layer;
        private TinyEntityGroup.Reference m_EntityGroup;
        private TinyEntityInstance m_Instance;

        public bool Enabled
        {
            get { return EnabledProperty.GetValue(this); }
            set { EnabledProperty.SetValue(this, value); }
        }

        public bool Static
        {
            get { return StaticProperty.GetValue(this); }
            set { StaticProperty.SetValue(this, value); }
        }

        public int Layer
        {
            get { return LayerProperty.GetValue(this); }
            set { LayerProperty.SetValue(this, value); }
        }

        public TinyEntityInstance Instance
        {
            get { return InstanceProperty.GetValue(this); }
            set { InstanceProperty.SetValue(this, value); }
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
