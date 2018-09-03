#pragma once

#include "CameraProvider.h"
#include "Unity/IUnityXRCamera.h"

class LifecycleProviderCamera : public IUnityLifecycleProvider
{
public:
    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    const CameraProvider& GetCameraProvider() const;
    CameraProvider& GetCameraProviderMutable();

private:
    CameraProvider m_CameraProvider;
};
