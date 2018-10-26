#pragma once

#include "CameraProvider.h"

#include "Unity/IUnityXRCamera.deprecated.h"

class LifecycleProviderCamera : public IUnityLifecycleProvider
{
public:
    LifecycleProviderCamera();

    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    const CameraProvider& GetCameraProvider() const;
    CameraProvider& GetCameraProviderMutable();

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRCameraInterface* unityInterface, const char* subsystemId);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);

    CameraProvider m_CameraProvider;
    IUnityXRCameraInterface* m_UnityInterface;
};
