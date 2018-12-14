
using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    internal partial class TinyDocumentation : IPropertyContainer
    {
        public static ValueClassProperty<TinyDocumentation, string> SummaryProperty { get; private set; }

        private static ClassPropertyBag<TinyDocumentation> s_PropertyBag { get; set; }

        public IPropertyBag PropertyBag => s_PropertyBag;

        private static void InitializeProperties()
        {
            SummaryProperty = new ValueClassProperty<TinyDocumentation, string>(
                "Summary"
                ,c => c.m_Summary
                ,(c, v) => c.m_Summary = v
            );
        }

        /// <summary>
        /// Implement this partial method to initialize custom properties
        /// </summary>
        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyDocumentation>(
                SummaryProperty
            );
        }

        static TinyDocumentation()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private string m_Summary = string.Empty;

        public string Summary
        {
            get { return SummaryProperty.GetValue(this); }
            set { SummaryProperty.SetValue(this, value); }
        }
    }
}
