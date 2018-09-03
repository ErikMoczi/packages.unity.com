using System;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Add this component to a <c>Camera</c> to copy the color camera's texture onto the background.
    /// </summary>
    /// <remarks>
    /// This is the component-ized version of <c>UnityEngine.XR.ARBackgroundRenderer</c>.
    /// </remarks>
    [DisallowMultipleComponent, RequireComponent(typeof(Camera))]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@1.0/api/UnityEngine.XR.ARFoundation.ARCameraBackground.html")]
    public sealed class ARCameraBackground : MonoBehaviour
    {
        [SerializeField]
        bool m_OverrideMaterial;

        /// <summary>
        /// When false, a material is generated automatically from the shader included in the platform-specific package.
        /// You may override this material if you wish. Normally, you don't need to do this.
        /// </summary>
        public bool overrideMaterial
        {
            get { return m_OverrideMaterial; }
            set { m_OverrideMaterial = value; }
        }

        [SerializeField]
        Material m_Material;

        /// <summary>
        /// The <c>Material</c> to use for background rendering.
        /// </summary>
        /// <remarks>
        /// If <c>null</c>, the <see cref="ARSession" /> will attempt to create a material using
        /// the shader provided by the platform plugin.
        /// </remarks>
        public Material material
        {
            get { return m_Material; }
            set
            {
                m_Material = value;

                backgroundRenderer.backgroundMaterial = m_Material;
                if (ARSubsystemManager.cameraSubsystem != null)
                    ARSubsystemManager.cameraSubsystem.Material = m_Material;
            }
        }

        /// <summary>
        /// Get the <c>ARBackgroundRenderer</c> which does the real work.
        /// </summary>
        public ARBackgroundRenderer backgroundRenderer { get; private set; }

        void SetupCameraIfNecessary()
        {
            if (m_CameraHasBeenSetup)
                backgroundRenderer.mode = ARRenderMode.MaterialAsBackground;

            if (m_OverrideMaterial)
            {
                if (backgroundRenderer.backgroundMaterial != material)
                    material = material;

                return;
            }

            var cameraSubsystem = ARSubsystemManager.cameraSubsystem;
            if (m_CameraSetupThrewException || m_CameraHasBeenSetup || (cameraSubsystem == null))
                return;

            // Try to create a material from the plugin's provided shader.
            string shaderName = "";
            if (!cameraSubsystem.TryGetShaderName(ref shaderName))
                return;

            var shader = Shader.Find(shaderName);
            if (shader == null)
            {
                // If an exception is thrown, then something is irrecoverably wrong.
                // Set this flag so we don't try to do this every frame.
                m_CameraSetupThrewException = true;

                throw new InvalidOperationException(string.Format(
                    "Could not find shader named \"{0}\" required for video overlay on camera subsystem named \"{1}\".",
                    shaderName,
                    cameraSubsystem.SubsystemDescriptor.id));
            }

            material = new Material(shader);
            m_CameraHasBeenSetup = (material != null);
            NotifyCameraSubsystem();
        }

        void OnCameraFrameReceived(ARCameraFrameEventArgs eventArgs)
        {
            SetupCameraIfNecessary();
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            backgroundRenderer = new ARBackgroundRenderer();
            if (!m_OverrideMaterial)
                m_Material = null;
        }

        void NotifyCameraSubsystem()
        {
            var cameraSubsystem = ARSubsystemManager.cameraSubsystem;
            if (cameraSubsystem != null)
            {
                cameraSubsystem.Camera = m_Camera;
                cameraSubsystem.Material = material;
            }
        }

        void OnEnable()
        {
            backgroundRenderer.mode = ARRenderMode.MaterialAsBackground;
            backgroundRenderer.camera = m_Camera;

            NotifyCameraSubsystem();
            ARSubsystemManager.cameraFrameReceived += OnCameraFrameReceived;
            ARSubsystemManager.systemStateChanged += OnSystemStateChanged;
        }

        void OnDisable()
        {
            backgroundRenderer.mode = ARRenderMode.StandardBackground;
            ARSubsystemManager.cameraFrameReceived -= OnCameraFrameReceived;
            ARSubsystemManager.systemStateChanged -= OnSystemStateChanged;
            m_CameraHasBeenSetup = false;
            m_CameraSetupThrewException = false;

            // Tell the camera subsystem to stop doing work
            var cameraSubsystem = ARSubsystemManager.cameraSubsystem;
            if (cameraSubsystem != null)
            {
                if (cameraSubsystem.Camera == m_Camera)
                    cameraSubsystem.Camera = null;

                if (cameraSubsystem.Material == material)
                    cameraSubsystem.Material = null;
            }
        }

        void OnSystemStateChanged(ARSystemStateChangedEventArgs eventArgs)
        {
            if (eventArgs.state < ARSystemState.SessionInitializing && backgroundRenderer != null)
                backgroundRenderer.mode = ARRenderMode.StandardBackground;
        }

        bool m_CameraHasBeenSetup;

        bool m_CameraSetupThrewException;

        Camera m_Camera;
    }
}
