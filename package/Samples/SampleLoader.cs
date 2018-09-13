using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.XR.Management;

using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;

namespace Samples
{
    /// <summary>
    /// Sample loader implentation showing how to create simple loader.
    /// </summary>
    // Uncomment this line to have your loader instantiatable by an end user.
    //[CreateAssetMenu(menuName = "XR/Loaders/Sample Loader")]
    public class SampleLoader : XRLoaderHelper
    {
        private static List<XRInputSubsystemDescriptor> s_InputSubsystemDescriptors =
            new List<XRInputSubsystemDescriptor>();

        public XRInputSubsystem inputSubsystem
        {
            get { return GetLoadedSubsystem<XRInputSubsystem>(); }
        }

        private SampleSettings GetSettings()
        {
            SampleSettings settings = null;
            // When running in the Unit Editor, we can a users customization of configuration data directly form
            // EditorBuildSettings. At runtime, we need to grab it from the static instance field instead.
            #if UNITY_EDITOR
                UnityEditor.EditorBuildSettings.TryGetConfigObject(SampleConstants.kSettingsKey, out settings);
            #else
                settings = SampleSettings.s_RuntimeInstance;
            #endif
            return settings;
        }

#region XRLoader API Implementation

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
#endregion

    }
}
