#pragma once

#include "RaycastProvider.h"
#include "Unity/IUnityXRRaycast.h"

class LifecycleProviderRaycast : public IUnityLifecycleProvider
{
public:
    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

private:
    RaycastProvider m_RaycastProvider;
};
