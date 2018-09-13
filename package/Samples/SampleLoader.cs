using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR.Management;

using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;

namespace UnityEngine.XR.Management.Sample
{
    [CreateAssetMenu(menuName = "XR/Loaders/Sample Loader")]
    public class SampleLoader : XRLoader
    {
        Dictionary<Type,IntegratedSubsystem> subsystemInstanceMap = new Dictionary<Type, IntegratedSubsystem>();

        public XRInputSubsystem inputSubsystem
        {
            get { return GetLoadedSubsystem<XRInputSubsystem>(); }
        }

        private SampleSettings GetSettings()
        {
            SampleSettings settings = null;
            #if UNITY_EDITOR
                UnityEditor.EditorBuildSettings.TryGetConfigObject(SampleConstants.kSettingsKey, out settings);
            #else
                settings = SampleSettings.s_RuntimeInstance;
            #endif
            return settings;
        }

        public override bool Initialize()
        {
            SampleSettings settings = GetSettings();
            if (settings != null)
            {
                // TODO: Pass settings off to plugin prior to subsystem init.
            }

            CreateSubsystem<XRInputSubsystemDescriptor, XRInputSubsystem>(s_InputSubsystemDescriptors, "InputSubsystemDescriptor");

            return false;
        }

        public override T GetLoadedSubsystem<T>()
        {
            Type subsystemType = typeof(T);
            IntegratedSubsystem subsystem;
            subsystemInstanceMap.TryGetValue(subsystemType, out subsystem);
            return subsystem as T;
        }

        public override bool Start()
        {
            StartSubsystem<XRInputSubsystem>();
            return true;
        }

        public override bool Stop()
        {
            StopSubsystem<XRInputSubsystem>();
            return true;
        }

        public override bool Deinitialize()
        {
            DestroySubsystem<XRInputSubsystem>();
            return true;
        }

        private void StartSubsystem<T>() where T : Subsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Start();
        }

        private void StopSubsystem<T>() where T : Subsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Stop();
        }

        private void DestroySubsystem<T>() where T : Subsystem
        {
            T subsystem = GetLoadedSubsystem<T>();
            if (subsystem != null)
                subsystem.Destroy();
        }

        private static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors =
            new List<XRInputSubsystemDescriptor>();

        private void CreateSubsystem<TDescriptor, TSubsystem>(List<TDescriptor> descriptors, string id)
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
