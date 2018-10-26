#include "LifecycleProviderPlane.h"
#include "SessionProvider.h"
#include "Utility.h"

LifecycleProviderPlane::LifecycleProviderPlane()
    : m_UnityInterface(nullptr)
    , m_PlaneProvider(m_UnityInterface)
{
}

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
    DoStart();
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderPlane::Stop(IUnitySubsystem* /*subsystem*/)
{
    DoStop();
}

UnitySubsystemErrorCode LifecycleProviderPlane::SetUnityInterfaceAndRegister(IUnityXRPlaneInterface* unityInterface, const char* subsystemId)
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

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderPlane::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderPlane* thiz = static_cast<LifecycleProviderPlane*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRPlaneProvider provider;
    thiz->m_PlaneProvider.PopulateCStyleProvider(provider);
    return thiz->m_UnityInterface->RegisterPlaneProvider(handle, &provider);
}

void UNITY_INTERFACE_API LifecycleProviderPlane::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderPlane::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    DoStart();
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderPlane::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    DoStop();
}

void LifecycleProviderPlane::DoStart()
{
    GetSessionProviderMutable().RequestStartPlaneRecognition();
}

void LifecycleProviderPlane::DoStop()
{
    GetSessionProviderMutable().RequestStopPlaneRecognition();
}
