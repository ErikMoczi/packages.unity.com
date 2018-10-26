#pragma once

#include "InputProvider.h"

class LifecycleProviderInput
{
public:
    LifecycleProviderInput();

    UnitySubsystemErrorCode SetUnityInterfaceAndRegister(IUnityXRInputInterface* unityInterface, const char* subsystemId);

private:
    UnitySubsystemErrorCode Initialize(UnitySubsystemHandle handle);
    void Shutdown(UnitySubsystemHandle handle);
    UnitySubsystemErrorCode Start(UnitySubsystemHandle handle);
    void Stop(UnitySubsystemHandle handle);

    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticInitialize(UnitySubsystemHandle handle, void* userData);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticStart(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticStop(UnitySubsystemHandle handle, void* userData);
    static void UNITY_INTERFACE_API StaticShutdown(UnitySubsystemHandle handle, void* userData);

    IUnityXRInputInterface* m_UnityInterface;
    InputProvider m_InputProvider;
};
