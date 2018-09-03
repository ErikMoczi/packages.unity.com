#include "LifecycleProviderReferencePoint.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderReferencePoint::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRReferencePointSubsystem* xrReferencePointSubsystem = static_cast<IUnityXRReferencePointSubsystem*>(subsystem);
    xrReferencePointSubsystem->RegisterReferencePointProvider(&m_ReferencePointProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

void LifecycleProviderReferencePoint::UNITY_INTERFACE_API Shutdown(IUnitySubsystem* /*subsystem*/)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderReferencePoint::Start(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable reference points - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderReferencePoint::Stop(IUnitySubsystem* /*subsystem*/)
{
    // can't enable/disable reference points - nothing to do on session configuration
}
