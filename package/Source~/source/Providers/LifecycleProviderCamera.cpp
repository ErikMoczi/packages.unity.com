#include "CameraImage/CameraImageAndroid.h"
#include "LifecycleProviderCamera.h"
#include "Utility.h"

LifecycleProviderCamera::LifecycleProviderCamera()
    : m_UnityInterface(nullptr)
{
    CameraImageApi::RegisterInterface();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderCamera::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRCameraSubsystem* xrCameraSubsystem = static_cast<IUnityXRCameraSubsystem*>(subsystem);
    xrCameraSubsystem->RegisterCameraProvider(&m_CameraProvider);
    m_CameraProvider.OnLifecycleInitialize();
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
{ }

UnitySubsystemErrorCode LifecycleProviderCamera::SetUnityInterfaceAndRegister(IUnityXRCameraInterface* unityInterface, const char* subsystemId)
{
    m_UnityInterface = unityInterface;

    UnityLifecycleProvider provider;
    std::memset(&provider, 0, sizeof(provider));

    provider.pluginData = this;
    provider.Initialize = &StaticInitialize;
    provider.Shutdown = &StaticShutdown;
    provider.Start = &StaticStart;
    provider.Stop = &StaticStop;

    return unityInterface->RegisterLifecycleProvider("UnityARCore", subsystemId, &provider);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderCamera::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderCamera* thiz = static_cast<LifecycleProviderCamera*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRCameraProvider provider;
    thiz->m_CameraProvider.PopulateCStyleProvider(provider);
    return thiz->m_UnityInterface->RegisterCameraProvider(handle, &provider);
}

void UNITY_INTERFACE_API LifecycleProviderCamera::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderCamera* thiz = static_cast<LifecycleProviderCamera*>(userData);
    if (thiz == nullptr)
        return;
    thiz->m_CameraProvider.OnLifecycleShutdown();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderCamera::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderCamera::StaticStop(UnitySubsystemHandle handle, void* userData)
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
