using System;

using UnityEngine;
using UnityEngine.XR.Management;

namespace Unity.XR.Oculus
{
    [System.Serializable]
    [XRConfigurationData("Oculus", "Unity.XR.Oculus.Settings")]
    public class OculusSettings : ScriptableObject
    {
        public enum StereoRenderingModes
        {
            MultiPass = 0,
            SinglePass,
            SinglePassInstanced
        }

        public enum StereoRenderingModesAndroid
        {
            MultiPass = 0,
            Multiview = 3
        }

        [SerializeField, Tooltip("Set the Stereo Rendering Method")]
        public StereoRenderingModes StereoRenderingMode;

        [SerializeField, Tooltip("Set the Stereo Rendering Method")]
        public StereoRenderingModesAndroid StereoRenderingModeAndroid;

        [SerializeField, Tooltip("Enable a shared depth buffer")]
        public bool SharedDepthBuffer;

        [SerializeField, Tooltip("Enable Oculus Dash Support")]
        public bool DashSupport;


        public ushort GetStereoReneringMode()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return (ushort)StereoRenderingModeAndroid;
# else
            return (ushort)StereoRenderingMode;
#endif
        }
#if !UNITY_EDITOR
		static OculusSettings s_Settings;

		public void Awake()
		{
			s_Settings = this;
		}
#endif

    }
}
