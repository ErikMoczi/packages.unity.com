using System;

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
        [Tooltip("The number of seconds to wait after the AR Session begins before considering the session timed out.")]
        float m_SessionTimeoutInSeconds = 2f;

        /// <summary>
        /// Get or set the session timeout.
        /// </summary>
        /// <remarks>
        /// If the session does not respond within this time,
        /// the subsystems are stopped and an error is assumed to have occurred.
        /// Listen for session state changes by subscribing to <see cref="ARSubsystemManager.sessionStateChanged"/>.
        /// </remarks>
        public float sessionTimeoutInSeconds
        {
            get { return m_SessionTimeoutInSeconds; }
            set { m_SessionTimeoutInSeconds = value; }
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

            if (ARSubsystemManager.sessionSubsystem == null)
                ARSubsystemManager.CreateSubsystems();

            ARSubsystemManager.lightEstimationRequested = lightEstimation;

            if (ARSubsystemManager.sessionSubsystem != null)
            {
                m_TimeoutCallback = ARSubsystemManager.StartSubsystems();
                m_SubsystemStartTime = Time.realtimeSinceStartup;
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
            DontDestroyOnLoad(this);
        }

        void Update()
        {
            if ((m_TimeoutCallback != null) && (ARSubsystemManager.sessionState == ARSessionState.Initializing))
            {
                var elapsedTime = Time.realtimeSinceStartup - m_SubsystemStartTime;
                if (elapsedTime > sessionTimeoutInSeconds)
                    m_TimeoutCallback();
            }
        }

        Action m_TimeoutCallback;

        float m_SubsystemStartTime;
    }
}
