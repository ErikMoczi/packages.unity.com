#pragma once
#if UNITY
#include "Runtime/PluginInterface/Headers/IUnityInterface.h"
#else
#include "IUnityInterface.h"
#endif

#include "stddef.h"

/// Empty base class from which all Unity Subsystems derive
struct IUnitySubsystem {};

/// Error codes for Subsystem operations
enum UnitySubsystemErrorCode
{
    /// Indicates a successful operation
    kUnitySubsystemErrorCodeSuccess,

    /// Indicates the operation failed
    kUnitySubsystemErrorCodeFailure
};

/// Event handler implemented by the plugin for subsystem specific lifecycle events.
struct IUnityLifecycleProvider
{
    /// Initialize the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @return kXRErrorCodeSuccess if successfully initialized, kXRErrorCodeFailure on error conditions.
    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* inst) = 0;

    /// Start the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* inst) = 0;

    /// Stop the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Stop(IUnitySubsystem* inst) = 0;

    /// Shutdown the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* inst) = 0;
};

/// Declare the Unity plugin interface for XR plugins
#define UNITY_XR_DECLARE_INTERFACE(Name) \
struct Name : public IUnityInterface \
{ \
bool (UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, IUnityLifecycleProvider* provider); \
}
