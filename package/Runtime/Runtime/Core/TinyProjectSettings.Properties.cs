using System;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public partial class TinyProjectSettings : IPropertyContainer
    {
        /// <summary>
        /// <see cref="TinyProjectSettings.CanvasWidth" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, int> CanvasWidthProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.CanvasHeight" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, int> CanvasHeightProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.EmbedAssets" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> EmbedAssetsProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.CanvasAutoResize" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> CanvasAutoResizeProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.RenderMode" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, RenderingMode> RenderModeProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.DefaultTextureSettings" /> property.
        /// </summary>
        public static ClassValueClassProperty<TinyProjectSettings, TinyTextureSettings> DefaultTextureSettingsProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.DefaultAudioClipSettings" /> property.
        /// </summary>
        public static ClassValueClassProperty<TinyProjectSettings, TinyAudioClipSettings> DefaultAudioClipSettingsProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.DefaultAnimationClipSettings" /> property.
        /// </summary>
        public static ClassValueClassProperty<TinyProjectSettings, TinyAnimationClipSettings> DefaultAnimationClipSettingsProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.LocalWSServerPort" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, int> LocalWSServerPortProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.LocalHTTPServerPort" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, int> LocalHTTPServerPortProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.SingleFileHtml" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> SingleFileHtmlProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.MemorySize" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, int> MemorySizeProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.IncludeWebPDecompressor" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> IncludeWebPDecompressorProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.RunBabel" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> RunBabelProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.MinifyJavaScript" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> MinifyJavaScriptProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.LinkToSource" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> LinkToSourceProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.SymbolsInReleaseBuild" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, bool> SymbolsInReleaseBuildProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.GooglePlayStoreUrl" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, string> GooglePlayStoreUrlProperty { get; private set; }
        /// <summary>
        /// <see cref="TinyProjectSettings.AppStoreUrl" /> property.
        /// </summary>
        public static ValueClassProperty<TinyProjectSettings, string> AppStoreUrlProperty { get; private set; }

        private static ClassPropertyBag<TinyProjectSettings> s_PropertyBag { get; set; }

        /// <inheritdoc cref="Unity.Properties.IPropertyContainer.PropertyBag" />
        public IPropertyBag PropertyBag => s_PropertyBag;

        private static void InitializeProperties()
        {
            CanvasAutoResizeProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "CanvasAutoResize"
                ,c => c.m_CanvasAutoResize
                ,(c, v) => c.m_CanvasAutoResize = v
            );

            RenderModeProperty = new ValueClassProperty<TinyProjectSettings, RenderingMode>(
                "RenderMode"
                ,c => c.m_RenderMode
                ,(c, v) => c.m_RenderMode = v
            );

            DefaultTextureSettingsProperty = new ClassValueClassProperty<TinyProjectSettings, TinyTextureSettings>(
                "DefaultTextureSettings"
                ,c => c.m_DefaultTextureSettings
                ,(c, v) => c.m_DefaultTextureSettings = v
            );

            DefaultAudioClipSettingsProperty = new ClassValueClassProperty<TinyProjectSettings, TinyAudioClipSettings>(
                "DefaultAudioClipSettings"
                ,c => c.m_DefaultAudioClipSettings
                ,(c, v) => c.m_DefaultAudioClipSettings = v
            );

            DefaultAnimationClipSettingsProperty = new ClassValueClassProperty<TinyProjectSettings, TinyAnimationClipSettings>(
                "DefaultAnimationClipSettings"
                ,c => c.m_DefaultAnimationClipSettings
                ,(c, v) => c.m_DefaultAnimationClipSettings = v
            );

            SingleFileHtmlProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "SingleFileHtml"
                ,c => c.m_SingleFileHtml
                ,(c, v) => c.m_SingleFileHtml = v
            );

            IncludeWebPDecompressorProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "IncludeWebPDecompressor"
                ,c => c.m_IncludeWebPDecompressor
                ,(c, v) => c.m_IncludeWebPDecompressor = v
            );

            RunBabelProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "RunBabel"
                ,c => c.m_RunBabel
                ,(c, v) => c.m_RunBabel = v
            );

            MinifyJavaScriptProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "MinifyJavaScript"
                ,c => c.m_MinifyJavaScript
                ,(c, v) => c.m_MinifyJavaScript = v
            );

            LinkToSourceProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "LinkToSource"
                ,c => c.m_LinkToSource
                ,(c, v) => c.m_LinkToSource = v
            );

            SymbolsInReleaseBuildProperty = new ValueClassProperty<TinyProjectSettings, bool>(
                "SymbolsInReleaseBuild"
                ,c => c.m_SymbolsInReleaseBuild
                ,(c, v) => c.m_SymbolsInReleaseBuild = v
            );

            GooglePlayStoreUrlProperty = new ValueClassProperty<TinyProjectSettings, string>(
                "GooglePlayStoreUrl"
                ,c => c.m_GooglePlayStoreUrl
                ,(c, v) => c.m_GooglePlayStoreUrl = v
            );

            AppStoreUrlProperty = new ValueClassProperty<TinyProjectSettings, string>(
                "AppStoreUrl"
                ,c => c.m_AppStoreUrl
                ,(c, v) => c.m_AppStoreUrl = v
            );
        }

        static partial void InitializeCustomProperties();

        private static void InitializePropertyBag()
        {
            s_PropertyBag = new ClassPropertyBag<TinyProjectSettings>(
                CanvasWidthProperty,
                CanvasHeightProperty,
                EmbedAssetsProperty,
                CanvasAutoResizeProperty,
                RenderModeProperty,
                DefaultTextureSettingsProperty,
                DefaultAudioClipSettingsProperty,
                DefaultAnimationClipSettingsProperty,
                LocalWSServerPortProperty,
                LocalHTTPServerPortProperty,
                SingleFileHtmlProperty,
                MemorySizeProperty,
                IncludeWebPDecompressorProperty,
                RunBabelProperty,
                MinifyJavaScriptProperty,
                LinkToSourceProperty,
                SymbolsInReleaseBuildProperty,
                GooglePlayStoreUrlProperty,
                AppStoreUrlProperty
            );
        }

        static TinyProjectSettings()
        {
            InitializeProperties();
            InitializeCustomProperties();
            InitializePropertyBag();
        }

        private int m_CanvasWidth;
        private int m_CanvasHeight;
        private bool m_EmbedAssets;
        private bool m_CanvasAutoResize;
        private RenderingMode m_RenderMode;
        private TinyTextureSettings m_DefaultTextureSettings;
        private TinyAudioClipSettings m_DefaultAudioClipSettings;
        private TinyAnimationClipSettings m_DefaultAnimationClipSettings;
        private int m_LocalWSServerPort = DefaultLocalWSServerPort;
        private int m_LocalHTTPServerPort = DefaultLocalHTTPServerPort;
        private bool m_SingleFileHtml;
        private int m_MemorySize = DefaultMemorySize;
        private bool m_IncludeWebPDecompressor;
        private bool m_RunBabel;
        private bool m_MinifyJavaScript = true;
        private bool m_LinkToSource = true;
        private bool m_SymbolsInReleaseBuild = false;
        private string m_GooglePlayStoreUrl;
        private string m_AppStoreUrl;

        /// <summary>
        /// Rendered output width, in pixels.
        /// </summary>
        public int CanvasWidth
        {
            get { return CanvasWidthProperty.GetValue(this); }
            set { CanvasWidthProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Rendered output height, in pixels.
        /// </summary>
        public int CanvasHeight
        {
            get { return CanvasHeightProperty.GetValue(this); }
            set { CanvasHeightProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Whether or not to embed (inline as data URLs) assets in the generated output by default. Each asset can override this setting.
        /// </summary>
        public bool EmbedAssets
        {
            get { return EmbedAssetsProperty.GetValue(this); }
            set { EmbedAssetsProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Whether or not the rendered output should resize according to the host canvas element size.
        /// </summary>
        public bool CanvasAutoResize
        {
            get { return CanvasAutoResizeProperty.GetValue(this); }
            set { CanvasAutoResizeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Initial player rendering mode.
        /// </summary>
        public RenderingMode RenderMode
        {
            get { return RenderModeProperty.GetValue(this); }
            set { RenderModeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Default texture export settings.
        /// </summary>
        public TinyTextureSettings DefaultTextureSettings
        {
            get { return DefaultTextureSettingsProperty.GetValue(this); }
            set { DefaultTextureSettingsProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Default audio clip export settings.
        /// </summary>
        public TinyAudioClipSettings DefaultAudioClipSettings
        {
            get { return DefaultAudioClipSettingsProperty.GetValue(this); }
            set { DefaultAudioClipSettingsProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Default animation clip export settings.
        /// </summary>
        public TinyAnimationClipSettings DefaultAnimationClipSettings
        {
            get { return DefaultAnimationClipSettingsProperty.GetValue(this); }
            set { DefaultAnimationClipSettingsProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Port used to establish a connection between the Editor and Tiny players.
        /// </summary>
        public int LocalWSServerPort
        {
            get { return LocalWSServerPortProperty.GetValue(this); }
            set { LocalWSServerPortProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Port used to serve Tiny content over HTTP.
        /// </summary>
        public int LocalHTTPServerPort
        {
            get { return LocalHTTPServerPortProperty.GetValue(this); }
            set { LocalHTTPServerPortProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Inline all JavaScript code in the output HTML file. Release configuration only.
        /// </summary>
        public bool SingleFileHtml
        {
            get { return SingleFileHtmlProperty.GetValue(this); }
            set { SingleFileHtmlProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Fixed Emscripten heap memory size, in MBs.
        /// </summary>
        public int MemorySize
        {
            get { return MemorySizeProperty.GetValue(this); }
            set { MemorySizeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Ensure that WebP textures can be decompressed on any platform. This impacts code size.
        /// </summary>
        public bool IncludeWebPDecompressor
        {
            get { return IncludeWebPDecompressorProperty.GetValue(this); }
            set { IncludeWebPDecompressorProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Transpile the JavaScript game code to portable ECMA5.
        /// </summary>
        public bool RunBabel
        {
            get { return RunBabelProperty.GetValue(this); }
            set { RunBabelProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Minify JavaScript game code. Release configuration only.
        /// </summary>
        public bool MinifyJavaScript
        {
            get { return MinifyJavaScriptProperty.GetValue(this); }
            set { MinifyJavaScriptProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Reference the generated JavaScript output file directly for faster iterations. Development configurations only.
        /// </summary>
        public bool LinkToSource
        {
            get { return LinkToSourceProperty.GetValue(this); }
            set { LinkToSourceProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Whether release builds should include symbols or not.  If they do not, they will also be stripped.
        /// </summary>
        public bool SymbolsInReleaseBuild
        {
            get { return SymbolsInReleaseBuildProperty.GetValue(this); }
            set { SymbolsInReleaseBuildProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Determines the Google Play store URL of the advertised product on the Android platform.
        /// </summary>
        public string GooglePlayStoreUrl
        {
            get { return GooglePlayStoreUrlProperty.GetValue(this); }
            set { GooglePlayStoreUrlProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Determines the Apple App store URL of the advertised product on the iOS platform.
        /// </summary>
        public string AppStoreUrl
        {
            get { return AppStoreUrlProperty.GetValue(this); }
            set { AppStoreUrlProperty.SetValue(this, value); }
        }
    }
}
