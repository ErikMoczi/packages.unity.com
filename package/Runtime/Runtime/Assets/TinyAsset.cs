
using System;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// Addressable Unity asset reference with export settings.
    /// </summary>
    internal partial class TinyAsset : IEquatable<TinyAsset>
    {
        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.VersionStorage"/>
        public IVersionStorage VersionStorage { get; }

        /// <summary>
        /// Addressable name. Defaults to the underlying <see cref="Object" /> name if any.
        /// </summary>
        public string Name
        {
            get
            {
                var name = NameProperty.GetValue(this);

                if (string.IsNullOrEmpty(name) && m_Object)
                {
                    return m_Object.name;
                }

                return name;
            }
            set => NameProperty.SetValue(this, value);
        }

        internal TinyAsset(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }

        internal TSettings CreateExportSettings<TSettings>()
            where TSettings : TinyAssetExportSettings, new()
        {
            var settings = new TSettings
            {
                VersionStorage = VersionStorage
            };

            ExportSettingsProperty.SetValue(this, settings);

            return settings;
        }

        internal void ClearExportSettings()
        {
            ExportSettingsProperty.SetValue(this, null);
        }
        
        /// <summary>
        /// Returns whether the value of the given object is equal to the current <see cref="TinyAsset" />.
        /// </summary>
        /// <param name="other">The object to test the value equality of.</param>
        /// <returns>true if the value of the given object is equal to that of the current object, or if they both
        /// reference the same Unity asset; otherwise, false.</returns>
        public bool Equals(TinyAsset other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || Equals(m_Object, other.m_Object);
        }

        /// <summary>
        /// Returns whether the value of the given object is equal to the current <see cref="TinyAsset" />.
        /// </summary>
        /// <param name="obj">The object to test the value equality of.</param>
        /// <returns>true if the value of the given object is equal to that of the current object; otherwise, false.</returns>
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((TinyAsset) obj);
        }
        
        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return (m_Object && m_Object != null ? m_Object.GetHashCode() : 0);
        }
    }
}
