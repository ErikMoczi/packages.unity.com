using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
    public abstract class XRLoaderHelper : XRLoader
    {
        /// <summary>
        /// Map of loaded susbsystems. Used so we don't always have to fo to XRSubsystemManger and do a manual
        /// search to find the instance we loaded.
        /// </summary>
        protected Dictionary<Type, ISubsystem> m_SubsystemInstanceMap = new Dictionary<Type, ISubsystem>();

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
            ISubsystem subsystem;
            m_SubsystemInstanceMap.TryGetValue(subsystemType, out subsystem);
            return subsystem as T;
        }

        /// <summary>
        /// Start a subsystem instance of a given type. Subsystem assumed to already be loaded from
        /// a previous call to CreateSubsystem
        /// </summary>
        ///
        /// <paramref name="T">A subclass of <see cref="ISubsystem">ISubsystem</see></paramref>
        protected void StartSubsystem<T>() where T : class, ISubsystem
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
        /// <paramref name="T">A subclass of <see cref="ISubsystem">ISubsystem</see></paramref>
        protected void StopSubsystem<T>() where T : class, ISubsystem
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
        /// <paramref name="T">A subclass of <see cref="ISubsystem">ISubsystem</see></paramref>
        protected void DestroySubsystem<T>() where T : class, ISubsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Destroy();
        }

        protected ISubsystem CreateIntegratedSubsystemHelper<TSubsystem>(IntegratedSubsystemDescriptor<TSubsystem> descriptor, string id)
            where TSubsystem : IntegratedSubsystem
        {
            ISubsystem ret = null;
            if (descriptor != null && String.Compare(descriptor.id, id, true) == 0)
            {
                ret = descriptor.Create();
            }
            return ret;
        }
    
        protected ISubsystem CreateSubsystemHelper<TSubsystem>(SubsystemDescriptor<TSubsystem> descriptor, string id)
            where TSubsystem : Subsystem
        {
            ISubsystem ret = null;
            if (descriptor != null && String.Compare(descriptor.id, id, true) == 0)
            {
                ret = descriptor.Create();
            }
            return ret;
        }

        /// <summary>
        /// Creates a native, integrated subsystem given a list of descriptors and a specific subsystem id.
        /// </summary>
        ///
        /// <paramref name="TDescriptor">The descriptor type being passed in.</paramref>
        /// <paramref name="TSubsystem">The subsystem type being requested</paramref>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        protected void CreateIntegratedSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : IntegratedSubsystemDescriptor
            where TSubsystem : IntegratedSubsystem
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");

            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);

            if (descriptors.Count > 0)
            {
                foreach (var descriptor in descriptors)
                {
                    IntegratedSubsystemDescriptor<TSubsystem> desc = descriptor as IntegratedSubsystemDescriptor<TSubsystem>;
                    if (desc != null && String.Compare(desc.id, id) == 0)
                    {
                        ISubsystem subsys = desc.Create();
                        if (subsys != null)
                        {
                            m_SubsystemInstanceMap[typeof(TSubsystem)] = subsys;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Creates a managed, standalone subsystem given a list of descriptors and a specific subsystem id.
        /// </summary>
        ///
        /// <paramref name="TDescriptor">The descriptor type being passed in.</paramref>
        /// <paramref name="TSubsystem">The subsystem type being requested</paramref>
        /// <param name="descriptors">List of TDescriptor instances to use for subsystem matching.</param>
        /// <param name="id">The identifier key of the particualr subsystem implementation being requested.</param>
        protected void CreateStandaloneSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
            where TDescriptor : SubsystemDescriptor
            where TSubsystem : Subsystem
        {
            if (descriptors == null)
                throw new ArgumentNullException("descriptors");

            SubsystemManager.GetSubsystemDescriptors<TDescriptor>(descriptors);

            if (descriptors.Count > 0)
            {
                foreach (var descriptor in descriptors)
                {
                    SubsystemDescriptor<TSubsystem> desc = descriptor as SubsystemDescriptor<TSubsystem>;
                    if (desc != null && String.Compare(desc.id, id) == 0)
                    {
                        ISubsystem subsys = desc.Create();
                        if (subsys != null)
                        {
                            m_SubsystemInstanceMap[typeof(TSubsystem)] = subsys;
                            break;
                        }
                    }
                }
            }
        }
    }
}
