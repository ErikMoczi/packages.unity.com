#pragma once
#if UNITY
#   include "Modules/XR/ProviderInterface/UnityXRTypes.h"
#   include "Modules/XR/ProviderInterface/IUnitySubsystem.h"
#   include "Modules/XR/ProviderInterface/UnityXRTrackable.h"
#else
#include "UnityXRTypes.h"
#include "IUnitySubsystem.h"
#include "UnityXRTrackable.h"
#endif


#include <cstddef>
#include <stdint.h>

/// A session helps manage the lifecycle of the Environment subsystem.
/// It can be configured, started, and stopped.
struct IUnityXRSessionProvider
{
    /// Invoked by Unity to retrieve the current tracking state
    /// @return An enum described the current tracking state
    virtual UnityXRTrackingState UNITY_INTERFACE_API GetTrackingState() = 0;

    /// Invoked by Unity at the start of a new frame. This will be called before
    /// any user scripts run, and before any other providers are queried.
    virtual void UNITY_INTERFACE_API BeginFrame() = 0;

    /// Invoked by Unity after user scripts and just before rendering. It is called
    /// before IUnityXRCameraProvider and IUnityXRInputProvider are queried to allow
    /// the plugin to provide consistent frame data, e.g. pose and camera texture.
    virtual void UNITY_INTERFACE_API BeforeRender() = 0;

    /// Invoked by Unity when the application goes into a paused state. Use this
    /// if your platform needs to manually suspend resources used for tracking.
    virtual void UNITY_INTERFACE_API ApplicationPaused() = 0;

    /// Invoked by Unity when the application exits a paused state. Use this if
    /// your platform needs to manually suspend resources used for tracking and
    /// you have to manually re-enable those resources when resuming.
    virtual void UNITY_INTERFACE_API ApplicationResumed() = 0;
};

struct IUnityXRSessionSubsystem : public IUnitySubsystem
{
    /// Registers a session provider which Unity may invoke start or stop an environment session.
    /// @param sessionProvider The IUnityXRSessionProvider which will service session requests.
    virtual void UNITY_INTERFACE_API RegisterSessionProvider(IUnityXRSessionProvider* sessionProvider) = 0;
};

UNITY_XR_DECLARE_INTERFACE(IUnityXRSessionInterface);
UNITY_REGISTER_INTERFACE_GUID(0xAB33E08701854536ULL, 0x90F772DD777E3676ULL, IUnityXRSessionInterface);
