using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public partial class TinyTextureSettings : IPropertyContainer
    {
        /// <summary>
        /// <see cref="TinyTextureSettings.FormatType" /> property.
        /// </summary>
        public static ValueClassProperty<TinyTextureSettings, TextureFormatType> FormatTypeProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyTextureSettings.JpgCompressionQuality" /> property.
        /// </summary>
        public static ValueClassProperty<TinyTextureSettings, int> JpgCompressionQualityProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyTextureSettings.WebPCompressionQuality" /> property.
        /// </summary>
        public static ValueClassProperty<TinyTextureSettings, int> WebPCompressionQualityProperty { get; private set; }

        private static ClassPropertyBag<TinyTextureSettings> s_PropertyBag { get; set; }

        private static void InitializeProperties()
        {
            FormatTypeProperty = new ValueClassProperty<TinyTextureSettings, TextureFormatType>(
                "FormatType"
                ,c => c.m_FormatType
                ,(c, v) => c.m_FormatType = v
            );

            JpgCompressionQualityProperty = new ValueClassProperty<TinyTextureSettings, int>(
                "JpgCompressionQuality"
                ,c => c.m_JpgCompressionQuality
                ,(c, v) => c.m_JpgCompressionQuality = v
            );

            WebPCompressionQualityProperty = new ValueClassProperty<TinyTextureSettings, int>(
                "WebPCompressionQuality"
                ,c => c.m_WebPCompressionQuality
                ,(c, v) => c.m_WebPCompressionQuality = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyTextureSettings>(
                TypeIdProperty,
                EmbeddedProperty,
                FormatTypeProperty,
                JpgCompressionQualityProperty,
                WebPCompressionQualityProperty
            );
        }

        static TinyTextureSettings()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private TextureFormatType m_FormatType = TextureFormatType.JPG;
        private int m_JpgCompressionQuality = 75;
        private int m_WebPCompressionQuality = 60;

        /// <summary>
        /// Export format of this texture.
        /// </summary>
        public TextureFormatType FormatType
        {
            get { return FormatTypeProperty.GetValue(this); }
            set { FormatTypeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// JPEG compression quality, from 0 to 100.
        /// </summary>
        public int JpgCompressionQuality
        {
            get { return JpgCompressionQualityProperty.GetValue(this); }
            set { JpgCompressionQualityProperty.SetValue(this, value); }
        }

        /// <summary>
        /// WebP compression quality, from 0 to 100.
        /// </summary>
        public int WebPCompressionQuality
        {
            get { return WebPCompressionQualityProperty.GetValue(this); }
            set { WebPCompressionQualityProperty.SetValue(this, value); }
        }
    }
}
