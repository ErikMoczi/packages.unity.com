#pragma once

#include "InputProvider.h"

class LifecycleProviderInput : public IUnityLifecycleProvider
{
public:
    LifecycleProviderInput();
    ~LifecycleProviderInput();

    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    virtual void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    virtual UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    virtual void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

private:
    void ShutdownImpl();

    InputProvider m_InputProvider;
    bool m_Initialized;
};
