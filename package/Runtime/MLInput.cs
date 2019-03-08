using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Lumin;
using UnityEngine.XR;
using UnityEngine.XR.MagicLeap;
using UnityEngine.Experimental;
using UnityEngine.Experimental.XR;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.XR.MagicLeap
{
    [AddComponentMenu("AR/Magic Leap/ML Input")]
    [UsesLuminPrivilege("ControllerPose")]
    public sealed class MLInput : MonoBehaviour
    {
        void OnEnable()
        {
            CreateInputSubsystemIfNeeded();
            if (s_InputSubsystem == null)
            {
                enabled = false;
                return;
            }

            s_InputSubsystem.Start();
        }

        void OnDisable()
        {
            if (s_InputSubsystem != null)
                s_InputSubsystem.Stop();
        }

        void OnDestroy()
        {
            if (s_InputSubsystem != null)
            {
                s_InputSubsystem.Destroy();
                s_InputSubsystem = null;
            }
        }

        static void CreateInputSubsystemIfNeeded()
        {
            if (s_InputSubsystem != null)
                return;

            s_Descriptors.Clear();
            SubsystemManager.GetSubsystemDescriptors<XRInputSubsystemDescriptor>(s_Descriptors);

            if (s_Descriptors.Count > 0)
            {
                var descriptorToUse = s_Descriptors[0];
                if (s_Descriptors.Count > 1)
                {
                    Type typeOfD = typeof(XRInputSubsystemDescriptor);
                    Debug.LogWarningFormat("Found {0} {1}s. Using \"{2}\"",
                        s_Descriptors.Count, typeOfD.Name, descriptorToUse.id);
                }

                s_InputSubsystem = descriptorToUse.Create();
            }
        }

        static XRInputSubsystem s_InputSubsystem;
        static List<XRInputSubsystemDescriptor> s_Descriptors = new List<XRInputSubsystemDescriptor>();
    }
}
