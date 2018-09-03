#include "LifecycleProviderSession.h"
#include "Utility.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRSessionSubsystem* sessionSubsystem = static_cast<IUnityXRSessionSubsystem*>(subsystem);
    sessionSubsystem->RegisterSessionProvider(&m_SessionProvider);
    return DoInitialize();
}

void LifecycleProviderSession::UNITY_INTERFACE_API Shutdown(IUnitySubsystem* /*subsystem*/)
{
    DoShutdown();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::Start(IUnitySubsystem* /*subsystem*/)
{
    return DoStart();
}

void UNITY_INTERFACE_API LifecycleProviderSession::Stop(IUnitySubsystem* /*subsystem*/)
{
    DoStop();
}

const SessionProvider& LifecycleProviderSession::GetSessionProvider() const
{
    return m_SessionProvider;
}

SessionProvider& LifecycleProviderSession::GetSessionProviderMutable()
{
    return m_SessionProvider;
}

UnitySubsystemErrorCode LifecycleProviderSession::SetUnityInterfaceAndRegister(IUnityXRSessionInterface* cStyleInterface, const char* subsystemId)
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

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderSession* thiz = static_cast<LifecycleProviderSession*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    UnityXRSessionProvider provider;
    thiz->m_SessionProvider.PopulateCStyleProvider(provider);
    UnitySubsystemErrorCode registerErrorCode = thiz->m_UnityInterface->RegisterSessionProvider(handle, &provider);
    if (registerErrorCode != kUnitySubsystemErrorCodeSuccess)
        return kUnitySubsystemErrorCodeFailure;

    return thiz->DoInitialize();
}

void UNITY_INTERFACE_API LifecycleProviderSession::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderSession* thiz = static_cast<LifecycleProviderSession*>(userData);
    if (thiz == nullptr)
        return;

    thiz->DoShutdown();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderSession* thiz = static_cast<LifecycleProviderSession*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->DoStart();
}

void UNITY_INTERFACE_API LifecycleProviderSession::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderSession* thiz = static_cast<LifecycleProviderSession*>(userData);
    if (thiz == nullptr)
        return;

    thiz->DoStop();
}

UnitySubsystemErrorCode LifecycleProviderSession::DoInitialize()
{
    m_SessionProvider.OnLifecycleInitialize();
    return kUnitySubsystemErrorCodeSuccess;
}

void LifecycleProviderSession::DoShutdown()
{
    m_SessionProvider.OnLifecycleShutdown();
}

UnitySubsystemErrorCode LifecycleProviderSession::DoStart()
{
    m_SessionProvider.OnLifecycleStart();
    return kUnitySubsystemErrorCodeSuccess;
}

void LifecycleProviderSession::DoStop()
{
    m_SessionProvider.OnLifecycleStop();
}
