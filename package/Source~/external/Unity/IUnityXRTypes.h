#pragma once
#if UNITY
#include "Runtime/PluginInterface/Headers/IUnityInterface.h"
#else
#include "IUnityInterface.h"
#endif
#include <cstddef>

const unsigned int kUnityXRStringSize = 128;

struct IXRInstance {};

enum XRErrorCode
{
    kXRErrorCodeSuccess,
    kXRErrorCodeFailure,
};

/// Event handler implemented by the plugin for subsystem specific lifecycle events.
struct IXRLifecycleProvider
{
    /// Initialize the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @param inst Pointer to platform-specific application parameters.
    /// @return kXRErrorCodeSuccess if successfully initialized, kXRErrorCodeFailure on error conditions.
    virtual XRErrorCode UNITY_INTERFACE_API Initialize(IXRInstance* inst) = 0;

    /// Start the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    /// @return kXRErrorCodeSuccess if successfully started, kXRErrorCodeFailure on error conditions.
    virtual XRErrorCode UNITY_INTERFACE_API Start(IXRInstance* inst) = 0;

    /// Stop the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Stop(IXRInstance* inst) = 0;

    /// Shutdown the subsystem.
    ///
    /// @param inst Pointer to the current XR Instance which can be cast to the specific subsystem's implementation.
    virtual void UNITY_INTERFACE_API Shutdown(IXRInstance* inst) = 0;
};

/// Declare the Unity plugin interface for XR plugins
#define XR_DECLARE_INTERFACE(Name) \
struct Name : public IUnityInterface \
{ \
    bool (UNITY_INTERFACE_API * RegisterLifecycleProvider)(const char* pluginName, const char* id, IXRLifecycleProvider* provider); \
}

/// Simple 2-Element Float Vector
struct XRVector2
{
    float x;
    float y;

    const float* GetPtr() const { return &x; }
    float* GetPtr() { return &x; }
};

/// Simple 3-Element float vector
struct XRVector3
{
    float x;
    float y;
    float z;

    const float* GetPtr() const { return &x; }
    float* GetPtr() { return &x; }
};

/// Simple 4 Element Quaternion with indices ordered x, y, z, and w in order
struct XRVector4
{
    float x;
    float y;
    float z;
    float w;

    const float* GetPtr() const { return &x; }
    float* GetPtr() { return &x; }
};

/// A simple struct representing a point in space with position and orientation
struct XRPose
{
    XRVector3 position;
    XRVector4 orientation;
};

/// A 4x4 column-major matrix
struct UnityXRMatrix4x4
{
    /// The columns of the matrix
    XRVector4 columns[4];
};
