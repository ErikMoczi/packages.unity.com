#include "LifecycleProviderCamera.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderCamera::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRCameraSubsystem* xrCameraSubsystem = static_cast<IUnityXRCameraSubsystem*>(subsystem);
    xrCameraSubsystem->RegisterCameraProvider(&m_CameraProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderCamera::Shutdown(IUnitySubsystem* /*subsystem*/)
{
    m_CameraProvider.OnLifecycleShutdown();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderCamera::Start(IUnitySubsystem* /*subsystem*/)
{
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderCamera::Stop(IUnitySubsystem* /*subsystem*/)
{
}

const CameraProvider& LifecycleProviderCamera::GetCameraProvider() const
{
    return m_CameraProvider;
}

CameraProvider& LifecycleProviderCamera::GetCameraProviderMutable()
{
    return m_CameraProvider;
}
