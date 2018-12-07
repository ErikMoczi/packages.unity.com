using System;
using System.Collections;

using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.XR.Management
{

#if UNITY_EDITOR
    [InitializeOnLoad]
#endif
    public class XRGeneralSettings : ScriptableObject
    {
        public static string k_SettingsKey = "com.unity.xr.managment.loader_settings";
        internal static XRGeneralSettings s_RuntimeSettingsInstance = null;

        // TODO: Need to store a map of buildtarget to instance.
        [SerializeField]
        internal GameObject m_LoaderManagerInstance = null;

        XRManager m_XRManager = null;

        public static XRGeneralSettings Instance
        {
            get
            {
#if UNITY_EDITOR
                if (s_RuntimeSettingsInstance == null)
                {
                    EditorBuildSettings.TryGetConfigObject<XRGeneralSettings>(XRGeneralSettings.k_SettingsKey, out s_RuntimeSettingsInstance);
                }
#endif
                return s_RuntimeSettingsInstance;
            }
        }

        public GameObject LoaderManagerInstance
        {
            get
            {
                return m_LoaderManagerInstance;
            }
#if UNITY_EDITOR
            set
            {
                GameObject go = value as GameObject;
                if (go != null)
                {
                    XRManager goc = go.GetComponent<XRManager>() as XRManager;
                    if (goc == null)
                    {
                        Debug.LogError("Attempting to assing a game object intance that does not contaion an XRManager component on it.");
                        return;
                    }
                }

                m_LoaderManagerInstance = value;
            }
#endif            
        }

#if UNITY_EDITOR
        static XRGeneralSettings()
        {
            EditorApplication.playModeStateChanged += PlayModeStateChanged;
        }
#endif

#if !UNITY_EDITOR
        void Awake()
        {
            Debug.Log("XRGeneral Settings awakening...");
            s_RuntimeSettingsInstance = this;
            Application.quitting += Quit;
            DontDestroyOnLoad(s_RuntimeSettingsInstance);
        }
#endif

#if UNITY_EDITOR

        bool m_IsPlaying = false;

        static void PlayModeStateChanged(PlayModeStateChange state)
        {
            XRGeneralSettings instance = XRGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.InternalPlayModeStateChanged(state);
        }

        private void InternalPlayModeStateChanged(PlayModeStateChange state)
        {

            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    break;
                case PlayModeStateChange.EnteredPlayMode:
                    if (!m_IsPlaying)
                    {
                        InitXRSDK();
                        StartXRSDK();
                        m_IsPlaying = true;
                    }
                    break;
                case PlayModeStateChange.ExitingPlayMode:
                    if (m_IsPlaying)
                    {
                        m_IsPlaying = false;
                        StopXRSDK();
                        DeInitXRSDK();
                    }
                    break;
                case PlayModeStateChange.EnteredEditMode:
                    break;
            }
        }
#else
        static void Quit()
        {
            XRGeneralSettings instance = XRGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.OnDisable();
            instance.OnDestroy();                
        }

        void Start()
        {
            StartXRSDK();
        }

        void OnDisable()
        {
            StopXRSDK();
        }

        void OnDestroy()
        {
            DeInitXRSDK();
        }

#endif

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        internal static void AttemptInitializeXRSDKOnLoad()
        {
#if !UNITY_EDITOR
            XRGeneralSettings instance = XRGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.InitXRSDK();
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void AttemptStartXRSDKOnBeforeSplashScreen()
        {
#if !UNITY_EDITOR
            XRGeneralSettings instance = XRGeneralSettings.Instance;
            if (instance == null)
                return;

            instance.StartXRSDK();
#endif
        }

        private void InitXRSDK()
        {
            if (XRGeneralSettings.Instance == null || XRGeneralSettings.Instance.m_LoaderManagerInstance == null)
                return;

            m_XRManager = XRGeneralSettings.Instance.m_LoaderManagerInstance.GetComponent<XRManager>();
            if (m_XRManager == null)
            {
                Debug.LogError("Assigned GameObject for XR Management loading is invalid. XR SDK will not be automatically loaded.");
                return;
            }

            m_XRManager.automaticLoading = false;
            m_XRManager.automaticRunning = false;
            m_XRManager.InitializeLoaderSync();
        }

        private void StartXRSDK()
        {
            if (m_XRManager != null && XRManager.activeLoader != null)
            {
                m_XRManager.StartSubsystems();
            }
        }

        private void StopXRSDK()
        {
            if (m_XRManager != null && XRManager.activeLoader != null)
            {
                m_XRManager.StopSubsystems();
            }
        }

        private void DeInitXRSDK()
        {
            if (m_XRManager != null && XRManager.activeLoader != null)
            {
                m_XRManager.DeinitializeLoader();
                m_XRManager = null;
            }
        }

    }
}
