#pragma once

#include "PlaneProvider.h"
#include "Unity/IUnityXRPlane.h"

class LifecycleProviderPlane : public IUnityLifecycleProvider
{
public:
    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

private:
    PlaneProvider m_PlaneProvider;
};
