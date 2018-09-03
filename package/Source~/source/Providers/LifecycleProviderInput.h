#pragma once

#include "InputProvider.h"

class LifecycleProviderInput
{
public:
    LifecycleProviderInput()
    : m_InputInterface(nullptr)
    {}

    static UnitySubsystemErrorCode UNITY_INTERFACE_API Initialize(UnitySubsystemHandle handle, void* lifecycleProviderPtr);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API Start(UnitySubsystemHandle handle, void* lifecycleProviderPtr);
    static void UNITY_INTERFACE_API Stop(UnitySubsystemHandle handle, void* lifecycleProviderPtr);
    static void UNITY_INTERFACE_API Shutdown(UnitySubsystemHandle handle, void* lifecycleProviderPtr);

    void SetInputInterface(IUnityXRInputInterface* inputInterface);
    
    UnitySubsystemErrorCode InitializeImpl(UnitySubsystemHandle handle);
    void ShutdownImpl(UnitySubsystemHandle handle);
    UnitySubsystemErrorCode StartImpl(UnitySubsystemHandle handle);
    void StopImpl(UnitySubsystemHandle handle);

private:
    IUnityXRInputInterface* m_InputInterface;
    InputProvider m_InputProvider;
};
