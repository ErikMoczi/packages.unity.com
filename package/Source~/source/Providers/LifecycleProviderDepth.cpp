#include "LifecycleProviderDepth.h"
#include "Utility.h"

LifecycleProviderDepth::LifecycleProviderDepth()
    : m_UnityInterface(nullptr)
    , m_DepthProvider(m_UnityInterface)
{
}

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

UnitySubsystemErrorCode LifecycleProviderDepth::SetUnityInterfaceAndRegister(IUnityXRDepthInterface* cStyleInterface, const char* subsystemId)
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

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderDepth::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderDepth* thiz = static_cast<LifecycleProviderDepth*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRDepthProvider provider;
    thiz->m_DepthProvider.PopulateCStyleProvider(provider);
    return thiz->m_UnityInterface->RegisterDepthProvider(handle, &provider);
}

void UNITY_INTERFACE_API LifecycleProviderDepth::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderDepth::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable point clouds - nothing to do on session configuration
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderDepth::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    // can't enable/disable point clouds - nothing to do on session configuration
}
