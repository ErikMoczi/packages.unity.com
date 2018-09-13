using System.Collections;
using System.Collections.Generic;

using UnityEditor;

using UnityEngine;
using UnityEngine.XR.Management;

namespace UnityEngine.XR.Management
{

    /// <summary>
    /// Class to handle active loader and subsystem management for XR SDK. This class is to be added as a
    /// component on a GameObject in your scene. Given a list of loaders, it will attempt to load the first loader
    /// that is successful and store that as the active loader.
    ///
    /// Depending on configuration the component can do it all automatically at correct points in the scene lifecycle,
    /// or the use can manually manage the active loader lifecycle.
    ///
    /// Automatic lifecycle management is as follows
    ///
    ///  OnEnable -> <see cref="InitializeLoader"/>. The loader list will be iterated over and the first successful loader will be st
    ///              as the active loader.
    ///
    ///  Start -> <see cref="StartSubsystems"/>. Ask the active loader to start all subsystems.
    ///
    ///  OnDisable -> <see cref="StopSubsystems"/>. Ask the active loader to stop all subsystems.
    ///
    ///  OnDestroy -> <see cref="DeinitializeLoader"/>. Deinitialize and remove the active loader.
    /// </summary>
    public class XRManager : MonoBehaviour {

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
        /// Reeturn the current active loader, cast to the requested type. Useful shortcut when you need
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
        /// Note that there can only be one active loader. Any attempt to initialize a new active loader with one that
        /// is alread set will cause a warning to be logged and immediate exit of this function.
        /// </summary>
        public void InitializeLoader()
        {
            if (ActiveLoader != null)
            {
                Debug.LogWarning(
                    "XR Management has already initialized an active loader in this scene." +
                    "Please make sure to stop all subsystems and deinitialize the active loader before initializing a new one.");
                return;
            }

            foreach(var loader in Loaders)
            {
                if (loader != null && loader.Initialize())
                {
                    ActiveLoader = loader;
                    return;
                }
            }

            ActiveLoader = null;
        }

        /// <summary>
        /// If there is an active loader, this will request the loader to start all the subsystems that it
        /// is managing.
        /// </summary>
        public void StartSubsystems()
        {
            if (ActiveLoader != null)
            {
                ActiveLoader.Start();
            }
        }


        /// <summary>
        /// If there is an active loader, this will request the loader to stop all the subsystems that it
        /// is managing.
        /// </summary>
        public void StopSubsystems()
        {
            if (ActiveLoader != null)
            {
                ActiveLoader.Stop();
            }
        }

        /// <summary>
        /// If there is an active loader, this function will deintialize it and remove the active loader instance from
        /// management. We will automatically ca;ll <see cref="StopSubsystems"/> prior to deinitialization to make sure
        /// that things are cleaned up appropriately.
        /// </summary>
        public void DeinitializeLoader()
        {
            StopSubsystems();
            if (ActiveLoader != null)
            {
                ActiveLoader.Deinitialize();
                ActiveLoader = null;
            }
        }

        void OnEnable() {
            if (ManageActiveLoaderLifetime)
            {
                InitializeLoader();
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
