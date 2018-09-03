#pragma once

#include "SessionProvider.h"
#include "Unity/IUnityXRSession.deprecated.h"

class LifecycleProviderSession : public IUnityLifecycleProvider
{
public:
    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    const SessionProvider& GetSessionProvider() const;
    SessionProvider& GetSessionProviderMutable();

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRSessionInterface* cStyleInterface, const char* subsystemId);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);

private:
    UnitySubsystemErrorCode DoInitialize();
    void DoShutdown();

    UnitySubsystemErrorCode DoStart();
    void DoStop();

    IUnityXRSessionInterface* m_UnityInterface;
    SessionProvider m_SessionProvider;
};
