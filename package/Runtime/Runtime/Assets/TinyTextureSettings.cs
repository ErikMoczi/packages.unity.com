using Unity.Properties;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Texture export format.
    /// </summary>
    public enum TextureFormatType
    {
        /// <summary>
        /// The source asset is used as-is.
        /// </summary>
        Source,

        /// <summary>
        /// Standard PNG format
        /// </summary>
        PNG,

        /// <summary>
        /// Standard JPEG format
        /// </summary>
        JPG,

        /// <summary>
        /// Google WebP format
        /// </summary>
        WebP
    }

    /// <summary>
    /// Export settings for a texture asset.
    /// </summary>
    public sealed partial class TinyTextureSettings : TinyAssetExportSettings, ICopyable<TinyTextureSettings>
    {
        private static ValueClassProperty<TinyTextureSettings, TinyAssetTypeId> TypeIdProperty { get; set; }

        /// <inheritdoc cref="TinyAssetExportSettings.PropertyBag" />
        public override IPropertyBag PropertyBag => s_PropertyBag;

        static partial void InitializeCustomProperties()
        {
            TypeIdProperty = new ValueClassProperty<TinyTextureSettings, TinyAssetTypeId>("$TypeId",
                    c => TinyAssetTypeId.Texture,
                    null
                ).WithAttribute(HideInInspector)
                 .WithAttribute(Readonly);
        }

        /// <summary>
        /// Copies the properties from the given <see cref="TinyTextureSettings"/> object.
        /// </summary>
        /// <param name="other"><see cref="TinyTextureSettings"/> object to copy onto this instance.</param>
        public void CopyFrom(TinyTextureSettings other)
        {
            base.CopyFrom(other);

            m_FormatType = other.m_FormatType;
            m_JpgCompressionQuality = other.m_JpgCompressionQuality;
            m_WebPCompressionQuality = other.m_WebPCompressionQuality;

            VersionStorage.IncrementVersion(null, this);
        }
    }
}
