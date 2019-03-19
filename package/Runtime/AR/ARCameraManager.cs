using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.XR.ARSubsystems;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Manages the lifetime of the <c>XRCameraSubsystem</c>. Add one of these to a <c>Camera</c> in your scene
    /// if you want camera texture and light estimation information to be available.
    /// </summary>
    [DefaultExecutionOrder(ARUpdateOrder.k_CameraManager)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@2.0/api/UnityEngine.XR.ARFoundation.ARCameraManager.html")]
    public sealed class ARCameraManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The focus mode to request on the (physical) AR camera.")]
        CameraFocusMode m_FocusMode = CameraFocusMode.Auto;

        /// <summary>
        /// Get or set the <c>CameraFocusMode</c> to use on the camera
        /// </summary>
        public CameraFocusMode focusMode
        {
            get { return m_FocusMode; }
            set
            {
                m_FocusMode = value;
                if (enabled)
                    subsystem.focusMode = focusMode;
            }
        }

        [SerializeField]
        [Tooltip("When enabled, requests that light estimation information be made available.")]
        LightEstimationMode m_LightEstimationMode = LightEstimationMode.Disabled;

        /// <summary>
        /// Get or set whether light estimation information be made available (if possible).
        /// </summary>
        public LightEstimationMode lightEstimationMode
        {
            get { return m_LightEstimationMode; }
            set
            {
                m_LightEstimationMode = value;
                if (enabled && subsystem != null)
                    subsystem.lightEstimationMode = value;
            }
        }

        /// <summary>
        /// Determines whether camera permission has been granted.
        /// </summary>
        /// <value><c>true</c> if permission has been granted; otherwise, <c>false</c>.</value>
        public bool permissionGranted
        {
            get
            {
                if (subsystem != null)
                    return subsystem.permissionGranted;

                return false;
            }
        }

        /// <summary>
        /// An event which fires each time a new camera frame is received.
        /// </summary>
        public event Action<ARCameraFrameEventArgs> frameReceived;

        /// <summary>
        /// Gets the name of the shader used in background rendering.
        /// </summary>
        public string shaderName
        {
            get
            {
                if (subsystem != null)
                    return subsystem.shaderName;
                return null;
            }
        }

        /// <summary>
        /// Tries the get camera intrinsics. Camera intrinsics refers to properties
        /// of a physical camera which may be useful when performing additional
        /// computer vision processing on the camera image.
        /// </summary>
        /// <returns><c>true</c>, if <paramref name="cameraIntrinsics"/> was populated,
        /// <c>false</c> otherwise.</returns>
        /// <param name="cameraIntrinsics">The camera intrinsics to populate.</param>
        public bool TryGetIntrinsics(out XRCameraIntrinsics cameraIntrinsics)
        {
            if (subsystem == null)
            {
                cameraIntrinsics = default(XRCameraIntrinsics);
                return false;
            }

            return subsystem.TryGetIntrinsics(out cameraIntrinsics);
        }

        /// <summary>
        /// Get the <c>XRCameraSubsystem</c> whose lifetime this component manages.
        /// </summary>
        public XRCameraSubsystem subsystem { get; private set; }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
        }

        void OnEnable()
        {
            if (subsystem == null)
                subsystem = CreateSubsystem();

            if (subsystem != null)
            {
                subsystem.focusMode = m_FocusMode;
                subsystem.lightEstimationMode = m_LightEstimationMode;
                subsystem.Start();
            }
        }

        void Update()
        {
            if (subsystem == null)
                return;

            var cameraParams = new XRCameraParams
            {
                zNear = m_Camera.nearClipPlane,
                zFar = m_Camera.farClipPlane,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                screenOrientation = Screen.orientation
            };

            XRCameraFrame frame;
            if (subsystem.TryGetLatestFrame(cameraParams, out frame))
            {
                UpdateTexturesInfos();

                if (frameReceived != null)
                    InvokeFrameReceivedEvent(frame);
            }
        }

        void UpdateTexturesInfos()
        {
            var textureDescriptors = subsystem.GetTextureDescriptors(Allocator.Temp);
            try
            {
                int numUpdated = Math.Min(m_TextureInfos.Count, textureDescriptors.Length);

                // Update the existing textures that are in common between the two arrays.
                for (int i = 0; i < numUpdated; ++i)
                {
                    m_TextureInfos[i] = TextureInfo.GetUpdatedTextureInfo(m_TextureInfos[i], textureDescriptors[i]);
                }

                // If there are fewer textures in the current frame than we had previously, destroy any remaining unneeded
                // textures.
                if (numUpdated < m_TextureInfos.Count)
                {
                    for (int i = numUpdated; i < m_TextureInfos.Count; ++i)
                    {
                        m_TextureInfos[i].DestroyTexture();
                    }
                    m_TextureInfos.RemoveRange(numUpdated, (m_TextureInfos.Count - numUpdated));
                }
                // Else, if there are more textures in the current frame than we have previously, add new textures for any
                // additional descriptors.
                else if (textureDescriptors.Length > m_TextureInfos.Count)
                {
                    for (int i = numUpdated; i < textureDescriptors.Length; ++i)
                    {
                        m_TextureInfos.Add(new TextureInfo(textureDescriptors[i]));
                    }
                }
            }
            finally
            {
                if (textureDescriptors.IsCreated)
                    textureDescriptors.Dispose();
            }
        }

        void OnDisable()
        {
            if (subsystem != null)
                subsystem.Stop();
        }

        void OnDestroy()
        {
            if (subsystem != null)
                subsystem.Destroy();

            subsystem = null;
        }

        void InvokeFrameReceivedEvent(XRCameraFrame frame)
        {
            var lightEstimation = new ARLightEstimationData();

            if (frame.hasAverageBrightness)
                lightEstimation.averageBrightness = frame.averageBrightness;

            if (frame.hasAverageColorTemperature)
                lightEstimation.averageColorTemperature = frame.averageColorTemperature;

            if (frame.hasColorCorrection)
                lightEstimation.colorCorrection = frame.colorCorrection;

            var eventArgs = new ARCameraFrameEventArgs();

            eventArgs.lightEstimation = lightEstimation;

            if (frame.hasTimestamp)
                eventArgs.timestampNs = frame.timestampNs;

            if (frame.hasProjectionMatrix)
                eventArgs.projectionMatrix = frame.projectionMatrix;

            if (frame.hasDisplayMatrix)
                eventArgs.displayMatrix = frame.displayMatrix;

            s_Textures.Clear();
            s_PropertyIds.Clear();
            foreach (var textureInfo in m_TextureInfos)
            {
                s_Textures.Add(textureInfo.texture);
                s_PropertyIds.Add(textureInfo.descriptor.propertyNameId);
            }

            eventArgs.textures = s_Textures;
            eventArgs.propertyNameIds = s_PropertyIds;

            frameReceived(eventArgs);
        }

        XRCameraSubsystem CreateSubsystem()
        {
            SubsystemManager.GetSubsystemDescriptors(s_SubsystemDescriptors);
            if (s_SubsystemDescriptors.Count > 0)
            {
                var descriptor = s_SubsystemDescriptors[0];
                if (s_SubsystemDescriptors.Count > 1)
                {
                    Debug.LogWarningFormat("Multiple {0} found. Using {1}",
                        typeof(XRCameraSubsystem).Name,
                        descriptor.id);
                }

                return descriptor.Create();
            }
            else
            {
                return null;
            }
        }

        static List<XRCameraSubsystemDescriptor> s_SubsystemDescriptors =
            new List<XRCameraSubsystemDescriptor>();

        static List<Texture2D> s_Textures = new List<Texture2D>();

        static List<int> s_PropertyIds = new List<int>();

        readonly List<TextureInfo> m_TextureInfos = new List<TextureInfo>();

        Camera m_Camera;

        /// <summary>
        /// Container that pairs a <see cref="Unity.XR.ARSubsystems.XRTextureDescriptor"/> that wraps a native texture
        /// object and a <c>Texture2D</c> that is created for the native texture object.
        /// </summary>
        struct TextureInfo
        {
            /// <summary>
            /// Constant for whether the texture is in a linear color space.
            /// </summary>
            /// <value>
            /// Constant for whether the texture is in a linear color space.
            /// </value>
            const bool k_TextureHasLinearColorSpace = false;

            /// <summary>
            /// Constructs the texture info with the given descriptor and material.
            /// </summary>
            /// <param name="descriptor">The texture descriptor wrapping a native texture object.</param>
            public TextureInfo(XRTextureDescriptor descriptor)
            {
                m_Descriptor = descriptor;
                m_Texture = CreateTexture(m_Descriptor);
            }

            /// <summary>
            /// The texture descriptor describing the metadata for the native texture object.
            /// </summary>
            /// <value>
            /// The texture descriptor describing the metadata for the native texture object.
            /// </value>
            public XRTextureDescriptor descriptor
            {
                get { return m_Descriptor; }
            }
            XRTextureDescriptor m_Descriptor;

            /// <summary>
            /// The Unity <c>Texture2D</c> object for the native texture.
            /// </summary>
            /// <value>
            /// The Unity <c>Texture2D</c> object for the native texture.
            /// </value>
            public Texture2D texture
            {
                get { return m_Texture; }
            }
            Texture2D m_Texture;

            /// <summary>
            /// Sets the current descriptor, and creates/updates the associated texture as appropriate.
            /// </summary>
            /// <param name="textureInfo">The texture info to update.</param>
            /// <param name="descriptor">The texture descriptor wrapping a native texture object.</param>
            public static TextureInfo GetUpdatedTextureInfo(TextureInfo textureInfo, XRTextureDescriptor descriptor)
            {
                // If the current and given descriptors are equal, exit early from this method.
                if (textureInfo.m_Descriptor.Equals(descriptor))
                {
                    return textureInfo;
                }

                // If there is a texture already and if the descriptors have identical texture metadata, we only need
                // to update the existing texture with the given native texture object.
                if ((textureInfo.m_Texture != null) && textureInfo.m_Descriptor.hasIdenticalTextureMetadata(descriptor))
                {
                    // Update the current descriptor with the given descriptor.
                    textureInfo.m_Descriptor = descriptor;

                    // Update the current texture with the native texture object.
                    textureInfo.m_Texture.UpdateExternalTexture(textureInfo.m_Descriptor.nativeTexture);
                }
                // Else, we need to create a new texture object.
                else
                {
                    // Update the current descriptor with the given descriptor.
                    textureInfo.m_Descriptor = descriptor;

                    // Replace the current texture with a newly created texture, and update the material.
                    textureInfo.DestroyTexture();
                    textureInfo.m_Texture = CreateTexture(textureInfo.m_Descriptor);
                }

                return textureInfo;
            }

            /// <summary>
            /// Destroys the texture, and sets the property to <c>null</c>
            /// </summary>
            public void DestroyTexture()
            {
                if (m_Texture != null)
                {
                    UnityEngine.Object.Destroy(m_Texture);
                    m_Texture = null;
                }
            }

            /// <summary>
            /// Create the texture object for the native texture wrapped by the valid descriptor.
            /// </summary>
            /// <param name="descriptor">The texture descriptor wrapping a native texture object.</param>
            /// <returns>
            /// If the descriptor is valid, the <c>Texture2D</c> object created from the texture descriptor. Otherwise,
            /// <c>null</c>.
            /// </returns>
            static Texture2D CreateTexture(XRTextureDescriptor descriptor)
            {
                if (!descriptor.valid)
                {
                    return null;
                }

                Texture2D texture = Texture2D.CreateExternalTexture(descriptor.width, descriptor.height,
                                                                    descriptor.format, (descriptor.mipmapCount != 0),
                                                                    k_TextureHasLinearColorSpace,
                                                                    descriptor.nativeTexture);

                // NB: SetWrapMode needs to be the first call here, and the value passed
                //     needs to be kTexWrapClamp - this is due to limitations of what
                //     wrap modes are allowed for external textures in OpenGL (which are
                //     used for ARCore), as Texture::ApplySettings will eventually hit
                //     an assert about an invalid enum (see calls to glTexParameteri
                //     towards the top of ApiGLES::TextureSampler)
                // reference: "3.7.14 External Textures" section of
                // https://www.khronos.org/registry/OpenGL/extensions/OES/OES_EGL_image_external.txt
                // (it shouldn't ever matter what the wrap mode is set to normally, since
                // this is for a pass-through video texture, so we shouldn't ever need to
                // worry about the wrap mode as textures should never "wrap")
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Bilinear;
                texture.hideFlags = HideFlags.HideAndDontSave;

                return texture;
            }
        }
    }
}
