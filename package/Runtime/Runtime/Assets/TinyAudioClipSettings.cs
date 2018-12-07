
using Unity.Properties;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Export settings for an audio clip asset.
    /// </summary>
    public sealed class TinyAudioClipSettings : TinyAssetExportSettings, ICopyable<TinyAudioClipSettings>
    {
        private static ValueClassProperty<TinyAudioClipSettings, TinyAssetTypeId> TypeIdProperty { get; }

        private static readonly IPropertyBag s_PropertyBag;
        
        /// <inheritdoc cref="TinyAssetExportSettings.PropertyBag"/>
        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        static TinyAudioClipSettings()
        {
            TypeIdProperty = new ValueClassProperty<TinyAudioClipSettings, TinyAssetTypeId>("$TypeId",
                    c => TinyAssetTypeId.AudioClip,
                    null
                ).WithAttribute(HideInInspector)
                 .WithAttribute(Readonly);
            
            s_PropertyBag = new PropertyBag(
                TypeIdProperty,
                EmbeddedProperty
            );
        }
        
        /// <summary>
        /// Copies the given object properties to this instance.
        /// </summary>
        /// <param name="other">Object to copy from.</param>
        public void CopyFrom(TinyAudioClipSettings other)
        {
            base.CopyFrom(other);
            VersionStorage.IncrementVersion(null, this);
        }
    }
}
