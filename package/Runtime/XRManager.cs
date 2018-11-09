using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.UIElements;
#else
using UnityEngine.Experimental.UIElements;
#endif
using UnityEngine.Serialization;
using UnityEngine.XR.Management;

namespace UnityEngine.XR.Management
{
    /// <summary>
    /// Class to handle active loader and subsystem management for XR SDK. This class is to be added as a
    /// component on a GameObject in your scene. Given a list of loaders, it will attempt to load each loader in
    /// the given order. The first loader that is successful wins and all remaining loaders are ignored. The loader
    /// that succeeds is accessible through the <see cref="activeLoader"/> property on the manager.
    ///
    /// Depending on configuration the XRManager component will automatically manage the active loader at correct points in the scene lifecycle.
    /// The user can override certain points in the active loader lifecycle and manually manage them by toggling the *Automatic Loading* and *Automatic Running*
    /// properties through the inspector UI. Disabling *Automatic Loading* implies the the user is responsibile for the full lifecycle
    /// of the manager. Toggling this to false also toggles automatic running to false.
    ///
    /// Disabling *Automatic Running* implies that the user is responsible for starting and stopping
    /// the <see cref="activeLoader"/> through the <see cref="StartSubsystems"/> and <see cref="StopSubsystems"/> APIs.
    ///
    /// Automatic lifecycle management is executed as follows
    ///
    /// * OnEnable -> <see cref="InitializeLoader"/>. The loader list will be iterated over and the first successful loader will be set as the active loader.
    /// * Start -> <see cref="StartSubsystems"/>. Ask the active loader to start all subsystems.
    /// * OnDisable -> <see cref="StopSubsystems"/>. Ask the active loader to stop all subsystems.
    /// * OnDestroy -> <see cref="DeinitializeLoader"/>. Deinitialize and remove the active loader.
    /// </summary>
    public sealed class XRManager : MonoBehaviour
    {
        [HideInInspector]
        bool m_InitializationComplete = false;

        [SerializeField]
        [Tooltip("Determines if the XR Manager instance is responsible for creating and destroying the appropriate loader instance.")]
        [FormerlySerializedAs("AutomaticLoading")]
        bool m_AutomaticLoading = true;

        /// <summary>
        /// Get and set Automatic Loading state for this manager. When this is true, the manager will automatically call
        /// <see cref="InitializeLoader"/> and <see cref="DeiitializeLoader"/> for you. When false <see cref="automaticRunning"/>
        /// is also set to false and remains that way. This means that disabling automatic loading disables all automatic behavior
        /// for the manager.
        /// </summary>
        public bool automaticLoading
        {
            get { return m_AutomaticLoading; }
            set { m_AutomaticLoading = value; }
        }

        [SerializeField]
        [Tooltip("Determines if the XR Manager instance is responsible for starting and stopping subsystems for the active loader instance.")]
        [FormerlySerializedAs("AutomaticRunning")]
        bool m_AutomaticRunning = true;

        /// <summary>
        /// Get and set automatic running state for this manager. When set to true the manager will call <see cref="StartSubsystems"/>
        /// and <see cref="StopSubsystems"/> APIs at appropriate times. When set to false, or when <see cref="automaticLoading"/> is false
        /// then it is up to the user of the manager to handle that same functionality.
        /// </summary>
        public bool automaticRunning
        {
            get { return m_AutomaticRunning; }
            set { m_AutomaticRunning = value; }
        }


        [SerializeField]
        [Tooltip("List of XR Loader instances arranged in desired load order.")]
        [FormerlySerializedAs("Loaders")]
        List<XRLoader> m_Loaders = new List<XRLoader>();

        /// <summary>
        /// List of loaders currently managed by this XR Manager instance.
        /// </summary>
        public List<XRLoader> loaders
        {
            get { return m_Loaders; }
        }


        /// <summary>
        /// Read only boolean letting us know if initialization is completed. Because initialization is
        /// handled as a Coroutine, people taking advantage of the auto-lifecycle management of XRManager
        /// will need to wait for init to complete before checking for an ActiveLoader and calling StartSubsystems.
        /// </summary>
        public bool isInitializationComplete
        {
            get { return m_InitializationComplete;  }
        }

        [HideInInspector]
        static XRLoader s_ActiveLoader = null;

        ///<summary>
        /// Return the current singleton active loader instance.
        ///
        ///</summary>
        [HideInInspector]
        public static XRLoader activeLoader { get { return s_ActiveLoader; } private set { s_ActiveLoader = value; }}

        /// <summary>
        /// Return the current active loader, cast to the requested type. Useful shortcut when you need
        /// to get the active loader as something less generic than XRLoader.
        /// </summary>
        ///
        /// <paramref name="T">< Requested type of the loader></paramref>
        ///
        /// <returns>< The active loader as requested type, or null.></returns>
        public static T ActiveLoaderAs<T>() where T : XRLoader
        {
            return activeLoader as T;
        }

        /// <summary>
        /// Iterate over the configured list of loaders and attempt to initialize each one. The first one
        /// that succeeds is set as the active loader and initiialization imediately terminates.
        ///
        /// When complete <see cref="isInitializationComplete"> will be set to true. This will mark that it is safe to
        /// call other parts of the API. This does not guarantee that init successfully create a loader. For that
        /// you need to check that ActiveLoader is not null.
        ///
        /// Note that there can only be one active loader. Any attempt to initialize a new active loader with one
        /// already set will cause a warning to be logged and immediate exit of this function.
        /// </summary>
        ///
        /// <returns>Enumerator marking the next spot to continue execution at.</returns>
        public IEnumerator InitializeLoader()
        {
            if (activeLoader != null)
            {
                Debug.LogWarning(
                    "XR Management has already initialized an active loader in this scene." +
                    "Please make sure to stop all subsystems and deinitialize the active loader before initializing a new one.");
                yield break;
            }

            foreach (var loader in loaders)
            {
                if (loader != null)
                {
                    if (loader.Initialize())
                    {
                        activeLoader = loader;
                        m_InitializationComplete = true;
                        yield break;
                    }
                }

                yield return null;
            }

            activeLoader = null;
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to start all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="isInitializationComplete"> to be set to true prior to calling this API.
        /// </summary>
        public void StartSubsystems()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to StartSubsystems without an initialized manager." +
                    "Please make sure wait for initialization to complete before calling this API.");
                return;
            }

            if (activeLoader != null)
            {
                activeLoader.Start();
            }
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to stop all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="isInitializationComplete"> to be set to tru prior to calling this API.
        /// </summary>
        public void StopSubsystems()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to StopSubsystems without an initialized manager." +
                    "Please make sure wait for initialization to complete before calling this API.");
                return;
            }

            if (activeLoader != null)
            {
                activeLoader.Stop();
            }
        }

        /// <summary>
        /// If there is an active loader, this function will deinitialize it and remove the active loader instance from
        /// management. We will automatically call <see cref="StopSubsystems"/> prior to deinitialization to make sure
        /// that things are cleaned up appropriately.
        ///
        /// You must wait for <see cref="isInitializationComplete"> to be set to tru prior to calling this API.
        ///
        /// Upon return <see cref="isInitializationComplete"> will be rest to false;
        /// </summary>
        public void DeinitializeLoader()
        {
            if (!m_InitializationComplete)
            {
                Debug.LogWarning(
                    "Call to DeinitializeLoader without an initialized manager." +
                    "Please make sure wait for initialization to complete before calling this API.");
                return;
            }

            StopSubsystems();
            if (activeLoader != null)
            {
                activeLoader.Deinitialize();
                activeLoader = null;
            }

            m_InitializationComplete = false;
        }

        void OnEnable()
        {
            if (automaticLoading)
            {
                StartCoroutine(InitializeLoader());
            }
        }

        // Use this for initialization
        void Start()
        {
            if (automaticLoading && automaticRunning)
            {
                StartSubsystems();
            }
        }

        void OnDisable()
        {
            if (automaticLoading && automaticRunning)
            {
                StopSubsystems();
            }
        }

        void OnDestroy()
        {
            if (automaticLoading)
            {
                DeinitializeLoader();
            }
        }
    }
}
