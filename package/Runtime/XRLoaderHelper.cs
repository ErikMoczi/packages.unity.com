using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.XR;
using UnityEngine.Experimental;

namespace UnityEngine.XR.Management
{

	/// <summary>
    /// XR Loader abstract subclass used as a base class for specific provider implementations. Class provides some
    /// helper logic that can be used to handle subsystem handling in a typesafe manner, reducing potential boilerplate
    /// code.
    /// </summary>
    public abstract class XRLoaderHelper : XRLoader {

        /// <summary>
        /// Map of loaded susbsystems. Used so we don't always have to fo to XRSubsystemManger and do a manual
        /// search to find the instance we loaded.
        /// </summary>
        protected Dictionary<Type,IntegratedSubsystem> subsystemInstanceMap = new Dictionary<Type, IntegratedSubsystem>();

        /// <summary>
        /// Gets the loaded subsystem of the specified type. Implementation dependent as only implemetnations
        /// know what they have loaded and how best to get it..
        /// </summary>
        ///
        /// <paramref name="T">Type of the subsystem to get.</paramref>
        ///
        /// <returns>The loaded subsystem or null if not found.</returns>
        public override T GetLoadedSubsystem<T>()
        {
            Type subsystemType = typeof(T);
            IntegratedSubsystem subsystem;
            subsystemInstanceMap.TryGetValue(subsystemType, out subsystem);
            return subsystem as T;
        }

        /// <summary>
        /// Start a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <paramref name="T">A subclass of <see cref="IntegratedSubsystem">IntegratedSubsystem</see></paramref>
        protected void StartSubsystem<T>() where T : IntegratedSubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Start();
        }

        /// <summary>
        /// Stop a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <paramref name="T">A subclass of <see cref="IntegratedSubsystem">IntegratedSubsystem</see></paramref>
        protected void StopSubsystem<T>() where T : IntegratedSubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Stop();
        }

        /// <summary>
        /// Destroy a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <paramref name="T">A subclass of <see cref="IntegratedSubsystem">IntegratedSubsystem</see></paramref>
        protected void DestroySubsystem<T>() where T : IntegratedSubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Destroy();
        }

        /// <summary>
        /// Creates a subsystem given a list of descriptors and a specific subsystem id.
        /// </summary>
        ///
        /// <paramref name="TDescriptor">The descriptor type being passed in.</paramref>
        /// <paramref name="TSubsystem">The subsystem type being requested</paramref>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        protected void CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : IntegratedSubsystemDescriptor<TSubsystem>
            where TSubsystem : IntegratedSubsystem<TDescriptor>
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");

            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);

            if (descriptors.Count > 0)
            {
                foreach (var descriptor in descriptors)
                {
                    if (descriptor.id == id)
                    {
                        IntegratedSubsystem s = descriptor.Create();
                        subsystemInstanceMap[typeof(TSubsystem)] = s;
                    }
                }
            }
        }
	}
}
