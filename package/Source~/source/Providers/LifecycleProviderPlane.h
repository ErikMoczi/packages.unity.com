#pragma once

#include "PlaneProvider.h"

#include "Unity/IUnityXRPlane.deprecated.h"

class LifecycleProviderPlane : public IUnityLifecycleProvider
{
public:
    LifecycleProviderPlane();

    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRPlaneInterface* unityInterface, const char* subsystemId);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);

    static void DoStart();
    static void DoStop();

    IUnityXRPlaneInterface* m_UnityInterface;
    PlaneProvider m_PlaneProvider;
};
