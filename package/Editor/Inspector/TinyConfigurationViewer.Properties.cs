
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyConfigurationViewer : IPropertyContainer
    {
        private static ClassPropertyBag<TinyConfigurationViewer> s_PropertyBag { get; set; }

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
            s_PropertyBag = new ClassPropertyBag<TinyConfigurationViewer>();
        }

        static TinyConfigurationViewer()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }
    }
}
