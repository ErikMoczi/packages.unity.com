#include "LifecycleProviderReferencePoint.h"

LifecycleProviderReferencePoint::LifecycleProviderReferencePoint()
    : m_UnityInterface(nullptr)
    , m_ReferencePointProvider(m_UnityInterface)
{
}

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

UnitySubsystemErrorCode LifecycleProviderReferencePoint::SetUnityInterfaceAndRegister(IUnityXRReferencePointInterface* unityInterface, const char* subsystemId)
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

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderReferencePoint::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderReferencePoint* thiz = static_cast<LifecycleProviderReferencePoint*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRReferencePointProvider xrProvider;
    thiz->m_ReferencePointProvider.PopulateCStyleProvider(xrProvider);
    return thiz->m_UnityInterface->RegisterReferencePointProvider(handle, &xrProvider);
}

void UNITY_INTERFACE_API LifecycleProviderReferencePoint::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderReferencePoint::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable reference points - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderReferencePoint::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable reference points - nothing to do on session configuration
}
