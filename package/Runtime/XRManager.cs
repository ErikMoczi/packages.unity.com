using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.XR.Management;

namespace UnityEngine.XR.Management
{

    /// <summary>
    /// Class to handle active loader and subsystem management for XR SDK. This class is to be added as a
    /// component on a GameObject in your scene. Given a list of loaders, it will attempt to load each loader in
    /// the given order. The first loaded that is successful wins and all remaining loaders are ignored. The loader
    /// that succeeds is accessible through the #ActiveLoader property on the manager.
    ///
    /// Depending on configuration the component automatically manage the active loader at correct points in the scene lifecycle.
    /// The user override certain points in the active loader lifecycle and manually mangae them by toggling the *Manage Active Loader Lifetime* and *Mange Subsystems*
    /// properties through the inspector UI. Disabling *Manage Active Loader Lifetime* implies the the user is responsibile for initialization and de-initialization
    /// of the <see cref="ActiveLoader"/> using <see cref="InitializeLoader"/> and <see cref="DeinitializeLoader"/> APIs. Disabling *Mange Subsystems* implies that the user is responsible for starting and stopping
    /// the <see cref="ActiveLoader"/> through the <see cref="StartSubsystems"/> and <see cref="StopSubsystems"/> APIs.
    ///
    /// Automatic lifecycle management is as follows
    ///
    /// * OnEnable -> <see cref="InitializeLoader"/>. The loader list will be iterated over and the first successful loader will be set as the active loader.
    /// * Start -> <see cref="StartSubsystems"/>. Ask the active loader to start all subsystems.
    /// * OnDisable -> <see cref="StopSubsystems"/>. Ask the active loader to stop all subsystems.
    /// * OnDestroy -> <see cref="DeinitializeLoader"/>. Deinitialize and remove the active loader.
    /// </summary>
    public sealed class XRManager : MonoBehaviour
    {
        [HideInInspector]
        private bool m_InitializationComplete = false;

        [SerializeField]
        [Tooltip("Determines if the XR Manager instance is responsible for creating and destroying the apporpriate loader instance.")]
        private bool ManageActiveLoaderLifetime = true;

        [SerializeField]
        [Tooltip("Determines if the XR Manager instance is responsible for starting and stopping subsystems for the active loader instance.")]
        private bool ManageSubsystems = true;

        [SerializeField]
        [Tooltip("List of XR Loader instances arranged in desired load order.")]
        List<XRLoader> Loaders = new List<XRLoader>();


        /// <summary>
        /// Read only boolean letting us no if initialization is completed. Because initialization is
        /// handled as a CoRoutine, people taking advantage of the auto-lifecycle management of XRManager
        /// will need to wait for init to complete before check for an ActiveLoader and calling StartSubsystems.
        /// </summary>
        public bool IsInitializationComplete
        {
            get { return m_InitializationComplete;  }
        }

        /// <summary>
        /// Private handle to the one static laoader instance that should ever be running at
        /// any time.
        /// </summary>
        [HideInInspector]
        private static XRLoader s_ActiveLoader = null;

        /// Return the current singleton active loader instance.
        ///
        [HideInInspector]
        public static XRLoader ActiveLoader { get { return s_ActiveLoader; } private set { s_ActiveLoader = value; }}

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
            return ActiveLoader as T;
        }

        /// <summary>
        /// Iterate over the configured list of loaders and attempt to initialize each one. The first one
        /// that succeeds is set as the active loader and initiialization imediately terminates.
        ///
        /// When complete <see cref="IsInitializationComplete"> will be set to true. This will mark that it is safe to
        /// call other parts of the API. This does not guarantee that init successfully create a loader. For that
        /// you need to check that ActiveLoader is not null.
        ///
        /// Note that there can only be one active loader. Any attempt to initialize a new active loader with one that
        /// is alread set will cause a warning to be logged and immediate exit of this function.
        /// </summary>
        ///
        /// <returns>Enumerator marking the next spot to continue execution at.</returns>
        public IEnumerator InitializeLoader()
        {
            if (ActiveLoader != null)
            {
                Debug.LogWarning(
                    "XR Management has already initialized an active loader in this scene." +
                    "Please make sure to stop all subsystems and deinitialize the active loader before initializing a new one.");
                yield break;
            }

            foreach(var loader in Loaders)
            {
                if (loader != null)
                {
                    if (loader.Initialize())
                    {
                        ActiveLoader = loader;
                        m_InitializationComplete = true;
                        yield break;
                    }
                }

                yield return null;
            }

            ActiveLoader = null;
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to start all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="IsInitializationComplete"> to be set to tru prior to calling this API.
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

            if (ActiveLoader != null)
            {
                ActiveLoader.Start();
            }
        }


        /// <summary>
        /// If there is an active loader, this will request the loader to stop all the subsystems that it
        /// is managing.
        ///
        /// You must wait for <see cref="IsInitializationComplete"> to be set to tru prior to calling this API.
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

            if (ActiveLoader != null)
            {
                ActiveLoader.Stop();
            }
        }

        /// <summary>
        /// If there is an active loader, this function will deintialize it and remove the active loader instance from
        /// management. We will automatically ca;ll <see cref="StopSubsystems"/> prior to deinitialization to make sure
        /// that things are cleaned up appropriately.
        ///
        /// You must wait for <see cref="IsInitializationComplete"> to be set to tru prior to calling this API.
        ///
        /// Upon return <see cref="IsInitializationComplete"> will be rest to false;
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
            if (ActiveLoader != null)
            {
                ActiveLoader.Deinitialize();
                ActiveLoader = null;
            }

            m_InitializationComplete = false;
        }

        void OnEnable() {
            if (ManageActiveLoaderLifetime)
            {
                StartCoroutine(InitializeLoader());
            }
        }

    	// Use this for initialization
    	void Start () {
            if (ManageSubsystems)
            {
                StartSubsystems();
            }
    	}

        void OnDisable() {
            if (ManageSubsystems)
            {
                StopSubsystems();
            }
        }

        void OnDestroy() {
            if (ManageActiveLoaderLifetime)
            {
                DeinitializeLoader();
            }
        }
    }
}
