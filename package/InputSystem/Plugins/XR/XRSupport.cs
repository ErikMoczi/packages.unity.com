using System;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Input.Plugins.XR
{
    static class XRUtilities
    {
        /// <summary>
        /// A simple Regex pattern that allows InputDeviceMatchers to match to any version of the XRInput interface.
        /// </summary>
        public const string kXRInterfaceMatchAnyVersion = "^(XRInput)";

        /// <summary>
        /// The initial, now deprecated interface for XRInput.  This version handles button packing for Android differently from current.
        /// </summary>
        public const string kXRInterfaceV1 = "XRInput";

        /// <summary>
        /// The current interface code sent with devices to identify as XRInput devices.
        /// </summary>
        public const string kXRInterfaceCurrent = "XRInputV1";
    }

    // Sync to UnityXRInputFeatureType in IUnityXRInput.h
    enum FeatureType
    {
        Custom = 0,
        Binary,
        DiscreteStates,
        Axis1D,
        Axis2D,
        Axis3D,
        Rotation,
    }

    // These structures are not explicitly assigned, but they are filled in via JSON serialization coming from matching structs in native.
#pragma warning disable 0649
    [Serializable]
    struct UsageHint
    {
        public string content;
    }

    //Sync to XRInputFeatureDefinition in XRInputDeviceDefinition.h
    [Serializable]
    struct XRFeatureDescriptor
    {
        public string name;
        public List<UsageHint> usageHints;
        public FeatureType featureType;
        public uint customSize;
    }

    // Sync to UnityXRInputDeviceRole in IUnityXRInput.h
    /// <summary>
    /// The generalized role that the device plays.  This can help in grouping devices by type (HMD, vs. hardware tracker vs. handed controller).
    /// </summary>
    public enum DeviceRole
    {
        Unknown = 0,
        Generic,
        LeftHanded,
        RightHanded,
        GameController,
        TrackingReference,
        HardwareTracker,
    }

    //Sync to XRInputDeviceDefinition in XRInputDeviceDefinition.h
    [Serializable]
    class XRDeviceDescriptor
    {
        public string deviceName;
        public string manufacturer;
        public string serialNumber;
        public DeviceRole deviceRole;
        public int deviceId;
        public List<XRFeatureDescriptor> inputFeatures;

        internal string ToJson()
        {
            return JsonUtility.ToJson(this);
        }

        internal static XRDeviceDescriptor FromJson(string json)
        {
            return JsonUtility.FromJson<XRDeviceDescriptor>(json);
        }
    }
#pragma warning restore 0649

    /// <summary>
    /// A small helper class to aid in initializing and registering XR devices and layout builders.
    /// </summary>
    public static class XRSupport
    {
        /// <summary>
        /// Registers all initial templates and the generalized layout builder with the InputSystem.
        /// </summary>
        public static void Initialize()
        {
            InputSystem.RegisterControlLayout<XRHMD>();
            InputSystem.RegisterControlLayout<XRController>();

            InputSystem.RegisterControlLayout<WMRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("Windows Mixed Reality HMD"));
            InputSystem.RegisterControlLayout<WMRSpatialController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("^(Spatial Controller)"));

            InputSystem.RegisterControlLayout<OculusHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("Oculus")
                    .WithProduct("^(Oculus Rift)"));
            InputSystem.RegisterControlLayout<OculusTouchController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("Oculus")
                    .WithProduct(@"((Oculus Touch Controller)|(^(OpenVR Controller\(Oculus Rift [a-zA-Z0-9]*)))"));
            InputSystem.RegisterControlLayout<OculusTrackingReference>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct(@"((Tracking Reference)|(^(Oculus Rift [a-zA-Z0-9]* \(Camera)))"));

            InputSystem.RegisterControlLayout<GearVRHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("Oculus HMD"));
            InputSystem.RegisterControlLayout<GearVRTrackedController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("^(Oculus Tracked Remote)"));

            InputSystem.RegisterControlLayout<DaydreamHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("Daydream HMD"));
            InputSystem.RegisterControlLayout<DaydreamController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithProduct("Daydream Controller"));

            InputSystem.RegisterControlLayout<ViveHMD>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(Vive[\.]?((Pro)|( MV)))"));
            InputSystem.RegisterControlLayout<ViveWand>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(OpenVR Controller\(Vive[\.]? Controller)"));
            InputSystem.RegisterControlLayout<ViveLighthouse>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("HTC")
                    .WithProduct(@"^(HTC V2-XD/XE)"));

            InputSystem.RegisterControlLayout<KnucklesController>(
                matches: new InputDeviceMatcher()
                    .WithInterface(XRUtilities.kXRInterfaceMatchAnyVersion)
                    .WithManufacturer("Valve")
                    .WithProduct(@"^(OpenVR Controller\(Knuckles)"));

            InputSystem.onFindControlLayoutForDevice += XRLayoutBuilder.OnFindControlLayoutForDevice;
        }
    }
}
