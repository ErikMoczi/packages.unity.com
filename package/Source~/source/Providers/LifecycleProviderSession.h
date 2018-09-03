#pragma once

#include "SessionProvider.h"
#include "Unity/IUnityXRSession.h"

class LifecycleProviderSession : public IUnityLifecycleProvider
{
public:
    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    const SessionProvider& GetSessionProvider() const;
    SessionProvider& GetSessionProviderMutable();

private:
    SessionProvider m_SessionProvider;
};
