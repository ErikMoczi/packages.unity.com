#pragma once
#if UNITY
#   include "Modules/XR/ProviderInterface/UnityXRTypes.h"
#   include "Modules/XR/ProviderInterface/IUnitySubsystem.h"
#else
#   include "UnityXRTypes.h"
#   include "IUnitySubsystem.h"
#   include <stdint.h>
#   include <stdbool.h>
#endif

#if !defined(UINT32_MAX)
#  define UINT32_MAX  4294967295UL
#endif

/// This extra interface is required to access a preliminiary preview of the XRInput subsystem available in 2018.1.  For 2018.2 and beyond, please use the standard IUnityXRInput header.
namespace UnityXRInput_V1
{
    /// An id used to identify individual devices associated with a specific Input Instance.  Must be unique for unique devices.
    typedef uint32_t UnityXRInternalInputDeviceId;
    
    /// Used to represent an invalid feature index
    const uint32_t kUnityInvalidXRInputFeatureIndex = UINT32_MAX;
    
    /// Currently supported Input feature types.  Each type represents a unique type of data coming from a device
    enum UnityXRInputFeatureType
    {
        kUnityXRInputFeatureTypeBinary = 0, ///< Boolean
        kUnityXRInputFeatureTypeAxis1D, ///< Float
        kUnityXRInputFeatureTypeAxis2D, ///< UnityXRVector2
        kUnityXRInputFeatureTypeAxis3D, ///< UnityXRVector3
        kUnityXRInputFeatureTypeRotation, ///< UnityXRVector4
        kUnityXRInputFeatureTypePose, ///< UnityXRPose
        
        kUnityXRInputFeatureTypeInvalid = UINT32_MAX, ///< Represents an invalid feature type
    };
    
    /// Device Roles are supplied for connected devices to help Unity identify the type of device and how it should be used by developers
    enum UnityXRInputDeviceRole
    {
        kUnityXRInputDeviceRoleUnknown = 0, ///< Default, not mapped to anything
        kUnityXRInputDeviceRoleGeneric, ///< Camera's and HMDs
        kUnityXRInputDeviceRoleLeftHanded, ///< The device represents the left hand
        kUnityXRInputDeviceRoleRightHanded, ///< The device represents the right hand
        
        kUnityXRInputDeviceRoleCount, ///< The number of device roles
        kUnityXRInputDeviceRoleInvalid = UINT32_MAX ///< Represents an invalid device role
    };
    
    /// Device Roles are supplied for connected devices to help Unity identify the type of device and how it should be used by developers
    enum UnityXRInputFeatureUsage
    {
        kUnityXRInputFeatureUsageNone = 0,
        kUnityXRInputFeatureUsageDevicePose, ///< Expects an XRPose
        kUnityXRInputFeatureUsageLeftEye, ///< Expects an XRPose
        kUnityXRInputFeatureUsageRightEye, ///< Expects an XRPose
        kUnityXRInputFeatureUsageCenterEye, ///< Expects an XRPose
        kUnityXRInputFeatureUsageIsTracked, ///< Expects a Boolean
        kUnityXRInputFeatureUsageColorCameraPose, ///< Expects an XRPose
        
        kUnityXRInputFeatureUsageCount, ///< The number of feature usages
        kUnityXRInputFeatureUsageInvalid = UINT32_MAX ///< Represents an invalid feature usage
    };
    
    /// A Definition of what your device is capable of doing.  Immutable information are provided here when Unity called IUnityXRInputProvider::FillDeviceDefinition with a specific Device Id.
    /// It will only be called once for each device connection.  For APIs that use and update the device definition see IUnityXRInputSubsystem functions prefixed with DeviceDefinition
    class IUnityXRInputDeviceDefinition
    {
    public:
        /// Sets the name of the device.  Used to inform users of what's connected. deviceName must not be null and must be shorter than kUnityXRStringSize.
        ///
        /// @param deviceName The name of the new device.
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetName(const char* deviceName) = 0;
        
        /// Identifies the Role of this device.  Roles identify how the device is expected to be used (e.g. is it a tracker, a hand controller, an HMD, or a Gamepad)
        /// For available roles, see UnityXRInputDeviceRole
        ///
        /// @param deviceRole The role you'd like to assign the device.
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetRole(UnityXRInputDeviceRole deviceRole) = 0;
        
        /// Reports an input feature of the device, supplying the name specific to this device, a suggested usage, and a type.  Use the returned index when setting that feature's value within the associated UnityXRInputDeviceState. Name must not be null and must be shorter than kUnityXRStringSize.
        ///
        /// @param name The name of this input feature
        /// @param usageHint For available usages, see UnityXRInputFeatureUsage
        /// @param featureType The type of feature this is, which dictates the space and properties of the input feature
        /// @returns The assigned index, to be used with UnityXRInputDeviceState when setting the value of the added input feature, or kUnityInvalidXRInputFeatureIndex on failure
        virtual unsigned int UNITY_INTERFACE_API AddFeature(const char* name, UnityXRInputFeatureUsage usageHint, UnityXRInputFeatureType featureType) = 0;
    };
    
    /// A Structure setup to hold your individual device's state.  This is customized based on what was filled in for that devices' IXRInputDeviceDefinition.
    /// The Feature Indices are in the same order that AddFeature was called when filling in your device's definition.
    class IUnityXRInputDeviceState
    {
    public:
        /// Sets the binary value at a specific feature index.  This is used for features of type UnityXRInputFeatureTypeBinary
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param binaryValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetBinaryValue(unsigned int featureIndex, const bool& binaryValue) = 0;
        
        /// Sets the binary value at a specific feature index.  This is used for features of type UnityXRInputFeatureTypeAxis1D
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param axis1DValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetAxisValue(unsigned int featureIndex, const float& axis1DValue) = 0;
        
        /// Sets the binary value at a specific feature index.  This is used for features of type UnityXRInputFeatureTypeAxis2D
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param axis2DValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetAxis2DValue(unsigned int featureIndex, const UnityXRVector2& axis2DValue) = 0;
        
        /// Sets the binary value at a specific feature index.  This is used for features of type UnityXRInputFeatureTypeAxis3D
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param axis3DValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetAxis3DValue(unsigned int featureIndex, const UnityXRVector3& axis3DValue) = 0;
        
        /// Sets the binary value at a specific feature index.  This is used for features of type UnityXRInputFeatureTypeRotation
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param rotationValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetRotationValue(unsigned int featureIndex, const UnityXRVector4& rotationValue) = 0;
        
        /// Sets the binary value at a specific feature index.  This is used for features of type XRInputFeatureType_Pose
        ///
        /// @param featureIndex The index in device state that the feature is located.
        /// @param poseValue The value you want to set the feature to
        /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
        virtual UnitySubsystemErrorCode UNITY_INTERFACE_API SetPoseValue(unsigned int featureIndex, const UnityXRPose& poseValue) = 0;
    };
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Plugin Interface
    // Unity->Plugin messaging
    struct IUnityXRInputProvider
    {
        /// Fill in your connected device information here when requested.  Used to create customized device states.
        ///
        /// @param deviceId The Id of the device that needs a definition filled
        /// @param deviceDefinition The definition to be filled by the plugin
        virtual void UNITY_INTERFACE_API FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition) = 0;
        
        /// Called by Unity when it needs a current device snapshot
        ///
        /// @param deviceId The Id of the device whose state is requested.
        /// @param deviceState The customized DeviceState to fill in.  The indices within this state match the order those input features were added from FillDeviceDefinition
        /// @return True if the deviceState was populated; false otherwise.
        virtual bool UNITY_INTERFACE_API UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState) = 0;
    };
    
    /////////////////////////////////////////////////////////////////////////////////////////////////////////
    // Unity instance of Plugin
    // Plugin->Unity messaging
    struct IUnityXRInputSubsystem : public IUnitySubsystem
    {
        /// Registers your plugin for events that are specific to the Input subsystem.
        ///
        /// @param provider The event handler which contains definitions for input subsystem events.
        virtual void UNITY_INTERFACE_API RegisterProvider(IUnityXRInputProvider* provider) = 0;
        
        /// Notifies Unity of newly connected device (Thread Safe)
        ///
        /// @param deviceId An internally unique identifier for this device.
        virtual void UNITY_INTERFACE_API DeviceConnected(UnityXRInternalInputDeviceId deviceId) = 0;
        
        /// Notifies Unity of a disconnected device (Thread Safe)
        ///
        /// @param deviceId The internally unique id supplied by the device when it reported connecting.
        virtual void UNITY_INTERFACE_API DeviceDisconnected(UnityXRInternalInputDeviceId deviceId) = 0;
    };
    
    UNITY_XR_DECLARE_INTERFACE(IUnityXRInputInterface);
}

UNITY_REGISTER_INTERFACE_GUID(0x47B2126E444A168FULL, 0xFC25C91BFC672316ULL, UnityXRInput_V1::IUnityXRInputInterface);
