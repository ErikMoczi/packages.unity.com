#include "LifecycleProviderRaycast.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderRaycast::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRRaycastSubsystem* xrRaycastSubsystem = static_cast<IUnityXRRaycastSubsystem*>(subsystem);
    xrRaycastSubsystem->RegisterRaycastProvider(&m_RaycastProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderRaycast::Shutdown(IUnitySubsystem* /*subsystem*/)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderRaycast::Start(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable raycasting - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderRaycast::Stop(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable raycasting - nothing to do on session configuration
}
