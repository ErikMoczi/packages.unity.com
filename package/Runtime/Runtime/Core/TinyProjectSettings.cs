

using System;
using Unity.Properties;

namespace Unity.Tiny
{
    /// <summary>
    /// Available rendering modes.
    /// </summary>
    public enum RenderingMode
    {
        /// <summary>
        /// Most efficient rendering mode is selected automatically.
        /// </summary>
        Auto, 
        
        /// <summary>
        /// HTML canvas rendering (2D rendering context) is selected explicitly.
        /// </summary>
        Canvas, 
        
        /// <summary>
        /// WebGL rendering is selected explicitly.
        /// </summary>
        WebGL
    };


    /// <summary>
    /// Placeholder implementation.
    /// 
    /// This class should be used as the root entry point for all project and platform settings. 
    /// 
    /// Currently this is used as a place to dump all settings for all platforms. 
    /// 
    /// </summary>
    public sealed partial class TinyProjectSettings : IPropertyContainer
    {
        static partial void InitializeCustomProperties()
        {
            CanvasWidthProperty = new ValueClassProperty<TinyProjectSettings, int>(
                "CanvasWidth",
                c => c.m_CanvasWidth,
                (c, v) => c.m_CanvasWidth = Math.Max(v, 1)
            );
            
            CanvasHeightProperty = new ValueClassProperty<TinyProjectSettings, int>(
                "CanvasHeight",
                c => c.m_CanvasHeight,
                (c, v) => c.m_CanvasHeight = Math.Max(v, 1)
            );
            
            EmbedAssetsProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "EmbedAssets",
                c => c.m_EmbedAssets,
                (c, v) => {
                    c.m_EmbedAssets = v;
                    c.DefaultTextureSettings.Embedded = v;
                    c.DefaultAudioClipSettings.Embedded = v;
                }
            );
            
            SymbolsInReleaseBuildProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "SymbolsInReleaseBuild",
                c => c.m_SymbolsInReleaseBuild,
                (c, v) => c.m_SymbolsInReleaseBuild = v
            );

            LocalWSServerPortProperty = new ValueClassProperty<TinyProjectSettings, int>(
                "LocalWSServerPort",
                c => c.m_LocalWSServerPort,
                (c, v) => c.m_LocalWSServerPort = Math.Max(Math.Min(v, MaxNetPort), MinNetPort)
            );
            
            LocalHTTPServerPortProperty = new ValueClassProperty<TinyProjectSettings, int>(
                "LocalHTTPServerPort",
                c => c.m_LocalHTTPServerPort,
                (c, v) => c.m_LocalHTTPServerPort = Math.Max(Math.Min(v, MaxNetPort), MinNetPort)
            );
            
            MemorySizeProperty = new ValueClassProperty<TinyProjectSettings, int>(
                "MemorySize",
                c => c.m_MemorySize,
                (c, v) => {
                    const int multiple = 16;
                    const int max = 2048 - 16;

                    // Clamp between multiple and max
                    v = Math.Min(Math.Max(v, multiple), max);

                    // Round up to multiple
                    v = v + 0xF & -0x10;

                    c.m_MemorySize = v;
                }
            );
        }

        internal const int MinNetPort = 1025; // 1024 or less is reserved for well known services
        internal const int MaxNetPort = 49151; // 49152 up to 65535 are reserved for ephemeral ports
        internal const int DefaultLocalWSServerPort = 17700;
        internal const int DefaultLocalHTTPServerPort = 19050;
        internal const int DefaultMemorySize = 16;

        public IVersionStorage VersionStorage { get; }

        public TinyProjectSettings(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
            m_DefaultTextureSettings = new TinyTextureSettings { VersionStorage = versionStorage };
            m_DefaultAudioClipSettings = new TinyAudioClipSettings { VersionStorage = versionStorage };
            m_DefaultAnimationClipSettings = new TinyAnimationClipSettings { VersionStorage = versionStorage };
            m_RenderMode = RenderingMode.Auto;
        }

        public TinyAssetExportSettings GetDefaultAssetExportSettings(Type type)
        {
            if (typeof(UnityEngine.Texture2D).IsAssignableFrom(type))
            {
                return DefaultTextureSettings;
            }
            
            if (typeof(UnityEngine.AudioClip).IsAssignableFrom(type))
            {
                return DefaultAudioClipSettings;
            }

            if (typeof(UnityEngine.AnimationClip).IsAssignableFrom(type))
            {
                return DefaultAnimationClipSettings;
            }

            return new TinyGenericAssetExportSettings { Embedded = m_EmbedAssets };
        }
    }
}

