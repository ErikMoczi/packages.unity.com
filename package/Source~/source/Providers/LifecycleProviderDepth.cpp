#include "LifecycleProviderDepth.h"
#include "Utility.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderDepth::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRDepthSubsystem* xrDepthSubsystem = static_cast<IUnityXRDepthSubsystem*>(subsystem);
    xrDepthSubsystem->RegisterDepthProvider(&m_DepthProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderDepth::Shutdown(IUnitySubsystem* /*subsystem*/)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderDepth::Start(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable point clouds - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderDepth::Stop(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable point clouds - nothing to do on session configuration
}
