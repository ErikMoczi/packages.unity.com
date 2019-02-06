
using Unity.Properties;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Generic export settings for any asset type.
    /// </summary>
    public sealed class TinyGenericAssetExportSettings : TinyAssetExportSettings, ICopyable<TinyGenericAssetExportSettings>
    {
        private static ValueClassProperty<TinyGenericAssetExportSettings, TinyAssetTypeId> TypeIdProperty { get; }

        private static readonly IPropertyBag s_PropertyBag;
        
        /// <inheritdoc cref="TinyAssetExportSettings.PropertyBag"/>
        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        static TinyGenericAssetExportSettings()
        {
            TypeIdProperty = new ValueClassProperty<TinyGenericAssetExportSettings, TinyAssetTypeId>("$TypeId",
                    c => TinyAssetTypeId.Generic,
                    null
                ).WithAttribute(HideInInspector)
                 .WithAttribute(Readonly);
            
            s_PropertyBag = new PropertyBag(
                TypeIdProperty,
                EmbeddedProperty);
        }
        
        /// <summary>
        /// Copies the given object properties to this instance.
        /// </summary>
        /// <param name="other">Object to copy from.</param>
        public void CopyFrom(TinyGenericAssetExportSettings other)
        {
            base.CopyFrom(other);
            VersionStorage.IncrementVersion(null, this);
        }
    }
}
