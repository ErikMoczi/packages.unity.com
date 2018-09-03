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

#include <stddef.h>
#include <stdint.h>

/// A session helps manage the lifecycle of the Environment subsystem.
/// It can be configured, started, and stopped.
typedef struct UnityXRSessionProvider
{
    /// Pointer which will be passed to every callback as the userData parameter.
    void* userData;

    /// Invoked by Unity to retrieve the current tracking state
    /// @param[in]  handle   Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRSessionInterface functions that accept it.
    /// @param[in]  userData User-specified data, supplied in this struct when
    ///                      passed to RegisterSessionProvider.
    /// @return An enum described the current tracking state
    UnityXRTrackingState(UNITY_INTERFACE_API * GetTrackingState)(UnitySubsystemHandle handle, void* userData);

    /// Invoked by Unity at the start of a new frame. This will be called before
    /// any user scripts run, and before any other providers are queried.
    /// @param[in]  handle   Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRSessionInterface functions that accept it.
    /// @param[in]  userData User-specified data, supplied in this struct when
    ///                      passed to RegisterSessionProvider.
    void(UNITY_INTERFACE_API * BeginFrame)(UnitySubsystemHandle handle, void* userData);

    /// Invoked by Unity after user scripts and just before rendering. It is called
    /// before IUnityXRCameraProvider and IUnityXRInputProvider are queried to allow
    /// the plugin to provide consistent frame data, e.g. pose and camera texture.
    /// @param[in]  handle   Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRSessionInterface functions that accept it.
    /// @param[in]  userData User-specified data, supplied in this struct when
    ///                      passed to RegisterSessionProvider.
    void(UNITY_INTERFACE_API * BeforeRender)(UnitySubsystemHandle handle, void* userData);

    /// Invoked by Unity when the application goes into a paused state. Use this
    /// if your platform needs to manually suspend resources used for tracking.
    /// @param[in]  handle   Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRSessionInterface functions that accept it.
    /// @param[in]  userData User-specified data, supplied in this struct when
    ///                      passed to RegisterSessionProvider.
    void(UNITY_INTERFACE_API * ApplicationPaused)(UnitySubsystemHandle handle, void* userData);

    /// Invoked by Unity when the application exits a paused state. Use this if
    /// your platform needs to manually suspend resources used for tracking and
    /// you have to manually re-enable those resources when resuming.
    /// @param[in]  handle   Handle that could instead be obtained from lifecycle
    ///                      events, pass this back as the first parameter to
    ///                      IUnityXRSessionInterface functions that accept it.
    /// @param[in]  userData User-specified data, supplied in this struct when
    ///                      passed to RegisterSessionProvider.
    void(UNITY_INTERFACE_API * ApplicationResumed)(UnitySubsystemHandle handle, void* userData);
} UnityXRSessionProvider;

// @brief XR interface for supplying a means of controlling a session
UNITY_DECLARE_INTERFACE(IUnityXRSessionInterface)
{
    /// Entry-point for getting callbacks when the reference-point subsystem is initialized / started / stopped / shutdown.
    ///
    /// Example usage:
    /// @code
    /// #include "IUnityXRSession.h"
    ///
    /// static IUnityXRSession* s_XrSession = NULL;
    ///
    /// extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
    /// UnityPluginLoad(IUnityInterfaces* unityInterfaces)
    /// {
    ///     s_XrSession = unityInterfaces->Get<IUnityXRSessionInterface>();
    ///     UnityLifecycleProvider sessiontLifecycleHandler =
    ///     {
    ///         NULL,  // This can be any object you want to be passed as userData to the following functions
    ///         &Lifecycle_Initialize,
    ///         &Lifecycle_Start,
    ///         &Lifecycle_Stop,
    ///         &Lifecycle_Shutdown
    ///     };
    ///     s_XrSession->RegisterLifecycleProvider("PluginName", "HandheldSessionForAR", &sessionLifecycleHandler);
    /// }
    /// @endcode
    ///
    /// @param[in] pluginName Name of the plugin which was listed in your UnitySubsystemsManifest.json.
    /// @param[in] id         ID of the subsystem that was listed in your UnitySubsystemsManifest.json.
    /// @param[in] provider   Callbacks to register.
    /// @return               Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, const UnityLifecycleProvider * provider);

    /// Registers your plugin for events that are specific to the Session subsystem.
    ///
    /// @param[in] handle   Handle that was obtained from lifecycle events.
    /// @param[in] provider The event handler which contains definitions for session
    ///                     subsystem events.
    /// @return             Error code describing the success or failure of the operation.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * RegisterSessionProvider)(UnitySubsystemHandle handle, const UnityXRSessionProvider * provider);
};
UNITY_REGISTER_INTERFACE_GUID(0xEEE08C99940E493AULL, 0x88DE7AF342E14469ULL, IUnityXRSessionInterface);
