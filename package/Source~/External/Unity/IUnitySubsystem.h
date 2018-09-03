#pragma once
#if UNITY
#include "Runtime/PluginInterface/Headers/IUnityInterface.h"
#else
#include "IUnityInterface.h"
#endif

#include "stddef.h"

/// Error codes for Subsystem operations
typedef enum UnitySubsystemErrorCode
{
    /// Indicates a successful operation
    kUnitySubsystemErrorCodeSuccess,

    /// Indicates the operation failed
    kUnitySubsystemErrorCodeFailure,

    /// Indicates invalid arguments were passed to the method
    kUnitySubsystemErrorCodeInvalidArguments
} UnitySubsystemErrorCode;

/// An UnitySubsystemHandle is an opaque type that's passed between the plugin and Unity. (C-API)
typedef void* UnitySubsystemHandle;

/// Event handler implemented by the plugin for subsystem specific lifecycle events (C-API).
typedef struct UnityLifecycleProvider
{
    /// Plugin-specific data pointer.  Every function in this structure will
    /// be passed this data by the Unity runtime.  The plugin should allocate
    /// an appropriate data container for any information that is needed when
    /// a callback function is called.
    void* pluginData;

    /// Initialize the subsystem.
    ///
    /// @param handle Handle for the current XR subsystem which can be passed to methods related to that subsystem.
    /// @param data Value of pluginData field when provider was registered.
    /// @return Status of function execution.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * Initialize)(UnitySubsystemHandle handle, void* data);

    /// Start the subsystem.
    ///
    /// @param handle Handle for the current XR subsystem which can be passed to methods related to that subsystem.
    /// @param data Value of pluginData field when provider was registered.
    /// @return Status of function execution.
    UnitySubsystemErrorCode(UNITY_INTERFACE_API * Start)(UnitySubsystemHandle handle, void* data);

    /// Stop the subsystem.
    ///
    /// @param handle Handle for the current XR subsystem which can be passed to methods related to that subsystem.
    /// @param data Value of pluginData field when provider was registered.
    void(UNITY_INTERFACE_API * Stop)(UnitySubsystemHandle handle, void* data);

    /// Shutdown the subsystem.
    ///
    /// @param handle Handle for the current XR subsystem which can be passed to methods related to that subsystem.
    /// @param data Value of pluginData field when provider was registered.
    void(UNITY_INTERFACE_API * Shutdown)(UnitySubsystemHandle handle, void* data);
} UnityLifecycleProvider;

#ifdef __cplusplus
/// Type used for the interface between an XR SDK plugin and the Unity runtime. (CPP-API)
/// An IUnitySubsystem can be casted to a specific Subsystem type.
struct IUnitySubsystem {};

/// Event handler implemented by the plugin for subsystem specific lifecycle events.
struct IUnityLifecycleProvider
{
    /// Initialize the subsystem.
    ///
    /// @param subsystem Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @return Status of function execution.
    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) = 0;

    /// Start the subsystem.
    ///
    /// @param subsystem Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @return Status of function execution.
    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) = 0;

    /// Stop the subsystem.
    ///
    /// @param subsystem Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) = 0;

    /// Shutdown the subsystem.
    ///
    /// @param subsystem Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) = 0;
};

/// Declare the Unity plugin CPP-style interface for XR plugins
#define UNITY_XR_DECLARE_INTERFACE(Name) \
struct Name : public IUnityInterface \
{ \
bool (UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, IUnityLifecycleProvider* provider); \
}
#endif
