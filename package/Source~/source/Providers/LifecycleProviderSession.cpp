#include "LifecycleProviderSession.h"

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::Initialize(IUnitySubsystem* subsystem)
{
    IUnityXRSessionSubsystem* sessionSubsystem = static_cast<IUnityXRSessionSubsystem*>(subsystem);
    sessionSubsystem->RegisterSessionProvider(&m_SessionProvider);
    m_SessionProvider.OnLifecycleInitialize();
    return kUnitySubsystemErrorCodeSuccess;
}

void LifecycleProviderSession::UNITY_INTERFACE_API Shutdown(IUnitySubsystem* /*subsystem*/)
{
    m_SessionProvider.OnLifecycleShutdown();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderSession::Start(IUnitySubsystem* /*subsystem*/)
{
    m_SessionProvider.OnLifecycleStart();
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderSession::Stop(IUnitySubsystem* /*subsystem*/)
{
    m_SessionProvider.OnLifecycleStop();
}

const SessionProvider& LifecycleProviderSession::GetSessionProvider() const
{
    return m_SessionProvider;
}

SessionProvider& LifecycleProviderSession::GetSessionProviderMutable()
{
    return m_SessionProvider;
}
