#include "LifecycleProviderInput.h"
#include "Utility.h"

const UnityXRInternalInputDeviceId kInputDeviceId = 0;

LifecycleProviderInput::LifecycleProviderInput()
    : m_UnityInterface(nullptr)
{
}

UnitySubsystemErrorCode LifecycleProviderInput::SetUnityInterfaceAndRegister(IUnityXRInputInterface* unityInterface, const char* subsystemId)
{
    m_UnityInterface = unityInterface;
    m_InputProvider.SetInputInterface(unityInterface);

    UnityLifecycleProvider provider;
    std::memset(&provider, 0, sizeof(provider));

    provider.pluginData = this;
    provider.Initialize = &StaticInitialize;
    provider.Shutdown = &StaticShutdown;
    provider.Start = &StaticStart;
    provider.Stop = &StaticStop;

    return unityInterface->RegisterLifecycleProvider("UnityARCore", subsystemId, &provider);
}

UnitySubsystemErrorCode LifecycleProviderInput::Initialize(UnitySubsystemHandle handle)
{
    UnityXRInputProvider xrProvider;
    m_InputProvider.PopulateCStyleProvider(xrProvider);
    m_UnityInterface->RegisterInputProvider(handle, &xrProvider);
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode LifecycleProviderInput::Start(UnitySubsystemHandle handle)
{
    m_UnityInterface->InputSubsystem_DeviceConnected(handle, 0);    
    return kUnitySubsystemErrorCodeSuccess;
}

void LifecycleProviderInput::Stop(UnitySubsystemHandle handle)
{
    m_UnityInterface->InputSubsystem_DeviceDisconnected(handle, 0);
}

void LifecycleProviderInput::Shutdown(UnitySubsystemHandle handle)
{}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput::StaticInitialize(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderInput* thiz = static_cast<LifecycleProviderInput*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->Initialize(handle);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput::StaticStart(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderInput* thiz = static_cast<LifecycleProviderInput*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->Start(handle);
}

void UNITY_INTERFACE_API LifecycleProviderInput::StaticStop(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(userData);
    return lifecycleProvider->Stop(handle);
}

void UNITY_INTERFACE_API LifecycleProviderInput::StaticShutdown(UnitySubsystemHandle handle, void* userData)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(userData);
    return lifecycleProvider->Shutdown(handle);
}
