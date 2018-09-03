#pragma once

#include "InputProvider_V2.h"

class LifecycleProviderInput_V2 : public IUnityLifecycleProvider
{
public:
    LifecycleProviderInput_V2();
    ~LifecycleProviderInput_V2();

    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    virtual void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    virtual void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

private:
    void ShutdownImpl();

    InputProvider_V2 m_InputProvider;
    bool m_Initialized;
};
