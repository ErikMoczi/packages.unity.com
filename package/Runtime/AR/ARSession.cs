using System.Collections;
using UnityEngine.SceneManagement;

namespace UnityEngine.XR.ARFoundation
{
    /// <summary>
    /// Controls the lifecycle and configuration options for an AR session. There
    /// is only one active session. If you have multiple <see cref="ARSession"/> components,
    /// they all talk to the same session and will conflict with each other.
    /// 
    /// Enabling or disabling the <see cref="ARSession"/> will start or stop the session,
    /// respectively.
    /// </summary>
    [DisallowMultipleComponent]
    public class ARSession : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("When enabled, requests that light estimation information be made available.")]
        bool m_LightEstimation;

        /// <summary>
        /// When enabled, requests that light estimation information be made available.
        /// Not all AR devices support light estimation.
        /// </summary>
        public bool lightEstimation
        {
            get { return m_LightEstimation; }
            set
            {
                m_LightEstimation = value;
                if (enabled)
                    ARSubsystemManager.lightEstimationRequested = value;
            }
        }

        [SerializeField]
        [Tooltip("When enabled, the session is not destroyed on scene change.")]
        bool m_PersistBetweenScenes;

        /// <summary>
        /// Get or set the persistence of this session across scenes.
        /// </summary>
        /// <remarks>
        /// If the <see cref="ARSession"/> is not persisted between scenes, then it
        /// will be destroyed during a scene change, and you may lose any detected
        /// features in the environment, such as planar surfaces. This may be undesirable,
        /// so you can keep the session alive by setting <see cref="ARSession.persistBetweenScenes"/>
        /// to <c>true</c>. If you do this, you should not have an active <see cref="ARSession"/>
        /// in the scene that gets loaded, or they could conflict with each other.
        /// </remarks>
        public bool persistBetweenScenes
        {
            get
            {
                return m_PersistBetweenScenes;
            }

            set
            {
                if (m_PersistBetweenScenes == value)
                    return;

                m_PersistBetweenScenes = value;

                if (m_PersistBetweenScenes)
                {
                    DontDestroyOnLoad(this);
                }
                else
                {
                    var currentScene = SceneManager.GetActiveScene();
                    SceneManager.MoveGameObjectToScene(gameObject, currentScene);
                }
            }
        }

        [SerializeField]
        [Tooltip("If enabled, the session will attempt to update a supported device if its AR software is out of date.")]
        bool m_TryToInstallUpdateIfNeeded = true;

        /// <summary>
        /// If the device supports AR but does not have the necessary software, some platforms
        /// allow prompting the user to install or update the software. If <c>tryToInstallUpdateIfNeeded</c>
        /// is true, a software update will be attempted. If the appropriate software is not installed
        /// or out of date, and <c>tryToInstallUpdateIfNeeded</c> is <c>false</c>, then AR will not be available.
        /// </summary>
        public bool tryToInstallUpdateIfNeeded
        {
            get { return m_TryToInstallUpdateIfNeeded; }
            set { m_TryToInstallUpdateIfNeeded = value; }
        }

        /// <summary>
        /// Emits a warning in the console if more than one active <see cref="ARSession"/>
        /// component is active. There is only a single, global AR Session; this
        /// component controls that session. If two or more <see cref="ARSession"/>s are
        /// simultaneously active, then they both issue commands to the same session.
        /// Although this can cause unintended behavior, it is not expressly forbidden.
        ///
        /// This method is expensive and should not be called frequently.
        /// </summary>
        void WarnIfMultipleARSessions()
        {
            var sessions = FindObjectsOfType<ARSession>();
            if (sessions.Length > 1)
            {
                // Compile a list of session names
                string sessionNames = "";
                foreach (var session in sessions)
                {
                    sessionNames += string.Format("\t{0}\n", session.name);
                }

                Debug.LogWarningFormat(
                    "Multiple active AR Sessions found. " +
                    "These will conflict with each other, so " +
                    "you should only have one active ARSession at a time. " +
                    "Found these active sessions:\n{0}", sessionNames);
            }
        }

        void OnEnable()
        {
#if DEBUG
            WarnIfMultipleARSessions();
#endif
            StartCoroutine(Initialize());
        }

        IEnumerator Initialize()
        {
            // Make sure we've checked for availability
            if (ARSubsystemManager.systemState <= SystemState.CheckingAvailability)
                yield return ARSubsystemManager.CheckAvailability();

            // Make sure we didn't get disabled while checking for availability
            if (!enabled)
                yield break;

            // Complete install if necessary
            if (((ARSubsystemManager.systemState == SystemState.NeedsInstall) && tryToInstallUpdateIfNeeded) ||
                (ARSubsystemManager.systemState == SystemState.Installing))
            {
                yield return ARSubsystemManager.Install();
            }

            // If we're still enabled and everything is ready, then start.
            if (ARSubsystemManager.systemState == SystemState.Ready && enabled)
            {
                ARSubsystemManager.lightEstimationRequested = lightEstimation;
                ARSubsystemManager.StartSubsystems();
            }
            else
            {
                enabled = false;
            }
        }

        void OnDisable()
        {
            ARSubsystemManager.StopSubsystems();
        }

        void OnDestroy()
        {
            ARSubsystemManager.DestroySubsystems();
        }

        void Awake()
        {
            if (persistBetweenScenes)
                DontDestroyOnLoad(this);

            ARSubsystemManager.CreateSubsystems();

            // Kick this off immediately so we have the answer as soon as possible
            StartCoroutine(ARSubsystemManager.CheckAvailability());
        }
    }
}
