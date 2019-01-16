using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.Experimental;

namespace UnityEngine.XR.Management
{
    /// <summary>
    /// XR Loader abstract class used as a base class for specific provider implementations. Providers should implement
    /// subclasses of this to provide specific initialization and management implementations that make sense for their supported
    /// scenarios and needs.
    /// </summary>
    public abstract class XRLoader : ScriptableObject
    {
        /// <summary>
        /// Initialize the loader. This should initialize all subsystems to support the desired runtime setup this
        /// loader represents.
        /// </summary>
        ///
        /// <returns>Whether or not initialization succeeded.</returns>
        public virtual bool Initialize() { return false; }

        /// <summary>
        /// Ask loader to start all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not all subsystems were successfully started.</returns>
        public virtual bool Start() { return false; }

        /// <summary>
        /// Ask loader to stop all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not all subsystems were successfully stopped.</returns>
        public virtual bool Stop() { return false; }

        /// <summary>
        /// Ask loader to deinitialize all initialized subsystems.
        /// </summary>
        ///
        /// <returns>Whether or not deinitialization succeeded.</returns>
        public virtual bool Deinitialize() { return false; }

        /// <summary>
        /// Gets the loaded subsystem of the specified type. Implementation dependent as only implemetnations
        /// know what they have loaded and how best to get it..
        /// </summary>
        ///
        /// <paramref name="T">< Type of the subsystem to get ></paramref>
        ///
        /// <returns>The loaded subsystem or null if not found.</returns>
        public abstract T GetLoadedSubsystem<T>()  where T : class, ISubsystem;
    }
}
