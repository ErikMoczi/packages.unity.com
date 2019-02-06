
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal abstract partial class TinyRegistryObjectBase : IPropertyContainer
    {
        public static ValueClassProperty<TinyRegistryObjectBase, TinyId> IdProperty { get; private set; }

        private static ClassPropertyBag<TinyRegistryObjectBase> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            IdProperty = new ValueClassProperty<TinyRegistryObjectBase, TinyId>(
                "Id"
                ,c => c.m_Id
                ,(c, v) => c.m_Id = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyRegistryObjectBase>(
                IdProperty
            );
        }

        static TinyRegistryObjectBase()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private TinyId m_Id;

        public TinyId Id
        {
            get { return IdProperty.GetValue(this); }
            set { IdProperty.SetValue(this, value); }
        }
    }
}
