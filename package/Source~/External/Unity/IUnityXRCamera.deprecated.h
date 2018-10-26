#pragma once
#include "IUnityXRCamera.h"

/// Event handler implemented by the plugin for Camera subsystem specific events.
struct IUnityXRCameraProvider
{
    /// Get the rendering data associated with the most recent AR frame.
    ///
    /// @param[in] paramsIn Input parameters about the Unity camera which may be necessary or helpful generating e.g., the projection matrix.
    /// @param[out] frameOut The output frame, including lighting estimation, projection matrix, and texture information.
    /// @return True if frameOut was filled out with the current frame data; false otherwise.
    virtual bool UNITY_INTERFACE_API GetFrame(const UnityXRCameraParams& paramsIn, UnityXRCameraFrame* frameOut) = 0;

    /// Invoked by Unity to request that light estimation be enabled or disabled whenever the session is active.
    /// This method allows the Unity developer to specify intent. Whether light estimation is actually available
    /// may then be queried with GetEnableLightEstimationAvailable below.
    ///
    /// @param[in] requested True if light estimation is requested, otherwise false.
    virtual void UNITY_INTERFACE_API SetLightEstimationRequested(bool requested) = 0;

    /// Invoked by Unity to retrieve the name of the shader that should be used during
    /// background rendering (video overlay)
    ///
    /// @param[in] shaderName The name of the shader
    /// @return True if the shader name was filled out; false otherwise
    virtual bool UNITY_INTERFACE_API GetShaderName(char(&shaderName)[kUnityXRStringSize]) = 0;
};

/// When the camera subsystem is initialized, you will get an IUnitySubsystem pointer which can be cast to an IXRCameraSubsystem
/// in order to further interact with the XR Camera subsystem.
struct IUnityXRCameraSubsystem : public IUnitySubsystem
{
    /// Registers your plugin for events that are specific to the Camera subsystem.
    ///
    /// @param cameraProvider The event handler which contains definitions for camera subsystem events.
    virtual void UNITY_INTERFACE_API RegisterCameraProvider(IUnityXRCameraProvider* cameraProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRCameraInterface_Deprecated);
UNITY_REGISTER_INTERFACE_GUID(0xB633A7C9398B4A95ULL, 0xB225399ED5A2328FULL, IUnityXRCameraInterface_Deprecated);
