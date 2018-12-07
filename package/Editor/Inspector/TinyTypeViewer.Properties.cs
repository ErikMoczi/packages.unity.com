
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyTypeViewer : IPropertyContainer
    {
        public static ValueClassProperty<TinyTypeViewer, TinyType> TypeProperty { get; private set; }

        private static ClassPropertyBag<TinyTypeViewer> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => null;

        private static void InitializeProperties()
        {
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyTypeViewer>(
                TypeProperty
            );
        }

        static TinyTypeViewer()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private TinyType m_Type;

        public TinyType Type
        {
            get { return TypeProperty.GetValue(this); }
            set { TypeProperty.SetValue(this, value); }
        }
    }
}
