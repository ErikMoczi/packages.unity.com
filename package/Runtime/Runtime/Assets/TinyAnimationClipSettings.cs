using Unity.Properties;
using static Unity.Tiny.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Export settings for an animation clip asset.
    /// </summary>
    public sealed class TinyAnimationClipSettings : TinyAssetExportSettings, ICopyable<TinyAnimationClipSettings>
    {
        private static ValueClassProperty<TinyAnimationClipSettings, TinyAssetTypeId> TypeIdProperty { get; }

        private static readonly IPropertyBag s_PropertyBag;

        /// <inheritdoc cref="TinyAssetExportSettings.PropertyBag"/>
        public override IPropertyBag PropertyBag => s_PropertyBag;

        static TinyAnimationClipSettings()
        {
            TypeIdProperty = new ValueClassProperty<TinyAnimationClipSettings, TinyAssetTypeId>("$TypeId",
                    c => TinyAssetTypeId.AnimationClip,
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
        public void CopyFrom(TinyAnimationClipSettings other)
        {
            base.CopyFrom(other);
            VersionStorage.IncrementVersion(null, this);
        }
    }
}