#pragma once

#include "RaycastProvider.h"
#include "Unity/IUnityXRRaycast.deprecated.h"

class LifecycleProviderRaycast : public IUnityLifecycleProvider
{
public:
    LifecycleProviderRaycast();

    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRRaycastInterface* cStyleInterface, const char* subsystemId);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);

private:
    IUnityXRRaycastInterface* m_UnityInterface;
    RaycastProvider m_RaycastProvider;
};
