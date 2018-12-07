using Unity.Properties;

namespace Unity.Tiny
{
    internal interface ICopyable<in T>
    {
        void CopyFrom(T original);
    }

    /// <summary>
    /// Code for each supported asset types. 
    /// </summary>
    internal enum TinyAssetTypeId : ushort
    {
        /// <summary>
        /// Unknown asset type.
        /// </summary>
        Unknown = 0,
        
        /// <summary>
        /// Any asset type.
        /// </summary>
        Generic = 1,
        
        /// <summary>
        /// Texture asset type.
        /// </summary>
        Texture = 2,
        
        /// <summary>
        /// AudioClip asset type.
        /// </summary>
        AudioClip = 3,

        /// <summary>
        /// AnimationClip asset type.
        /// </summary>
        AnimationClip = 4
    }
    
    /// <summary>
    /// Base class for <see cref="TinyAsset"/> export settings.
    /// </summary>
    public abstract partial class TinyAssetExportSettings
    {
        /// <inheritdoc cref="IPropertyContainer.PropertyBag"/>
        public abstract IPropertyBag PropertyBag { get; }
        
        /// <inheritdoc cref="IPropertyContainer.VersionStorage"/>
        public IVersionStorage VersionStorage { get; internal set; }

        /// <summary>
        /// Copies the given object properties to this instance.
        /// </summary>
        /// <param name="other">Object to copy from.</param>
        protected void CopyFrom(TinyAssetExportSettings other)
        {
            m_Embedded = other.Embedded;
        }
        
        internal TinyAssetExportSettings()
        {
            // cannot be extended outside this assembly and friends
        }
    }
}
