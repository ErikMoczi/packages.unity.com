#include "LifecycleProviderPlane.h"
#include "SessionProvider.h"
#include "Utility.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderPlane::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRPlaneSubsystem* xrPlaneSubsystem = static_cast<IUnityXRPlaneSubsystem*>(subsystem);
    xrPlaneSubsystem->RegisterPlaneProvider(&m_PlaneProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderPlane::Shutdown(IUnitySubsystem* /*subsystem*/)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderPlane::Start(IUnitySubsystem* /*subsystem*/)
{
    GetSessionProviderMutable().RequestStartPlaneRecognition();
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderPlane::Stop(IUnitySubsystem* /*subsystem*/)
{
    GetSessionProviderMutable().RequestStopPlaneRecognition();
}
