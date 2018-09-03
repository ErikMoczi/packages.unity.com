#include "LifecycleProviderRaycast.h"
#include "Utility.h"

LifecycleProviderRaycast::LifecycleProviderRaycast()
    : m_UnityInterface(nullptr)
    , m_RaycastProvider(m_UnityInterface)
{
}

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

UnitySubsystemErrorCode LifecycleProviderRaycast::SetUnityInterfaceAndRegister(IUnityXRRaycastInterface* cStyleInterface, const char* subsystemId)
{
    m_UnityInterface = cStyleInterface;

    UnityLifecycleProvider provider;
    std::memset(&provider, 0, sizeof(provider));

    provider.pluginData = this;
    provider.Initialize = &StaticInitialize;
    provider.Shutdown = &StaticShutdown;
    provider.Start = &StaticStart;
    provider.Stop = &StaticStop;

    return cStyleInterface->RegisterLifecycleProvider("UnityARCore", subsystemId, &provider);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderRaycast::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderRaycast* thiz = static_cast<LifecycleProviderRaycast*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRRaycastProvider provider;
    thiz->m_RaycastProvider.PopulateCStyleProvider(provider);
    return thiz->m_UnityInterface->RegisterRaycastProvider(handle, &provider);
}

void UNITY_INTERFACE_API LifecycleProviderRaycast::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderRaycast::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable raycasting - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderRaycast::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable raycasting - nothing to do on session configuration
}
