#pragma once

#include "DepthProvider.h"
#include "Unity/IUnityXRDepth.deprecated.h"

class LifecycleProviderDepth : public IUnityLifecycleProvider
{
public:
    LifecycleProviderDepth();

    UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Shutdown(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode UNITY_INTERFACE_API Start(IUnitySubsystem* subsystem) override;
    void UNITY_INTERFACE_API Stop(IUnitySubsystem* subsystem) override;

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRDepthInterface* cStyleInterface, const char* subsystemId);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);

private:
    IUnityXRDepthInterface* m_UnityInterface;
    DepthProvider m_DepthProvider;
};
