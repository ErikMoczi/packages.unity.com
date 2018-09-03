#include "LifecycleProviderInput.h"
#include "Utility.h"

const UnityXRInternalInputDeviceId kInputDeviceId = 0;

void LifecycleProviderInput::SetInputInterface(IUnityXRInputInterface* inputInterface)
{
    m_InputInterface = inputInterface;
    m_InputProvider.SetInputInterface(inputInterface);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput::Initialize(UnitySubsystemHandle handle, void* lifecycleProviderPtr)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(lifecycleProviderPtr);
    return lifecycleProvider->InitializeImpl(handle);
}

UnitySubsystemErrorCode LifecycleProviderInput::InitializeImpl(UnitySubsystemHandle handle)
{
    UnityXRInputProvider inputProvider;
    inputProvider.userData = &m_InputProvider;
    inputProvider.OnNewInputFrame = &InputProvider::OnNewInputFrame;
    inputProvider.FillDeviceDefinition = &InputProvider::FillDeviceDefinition;
    inputProvider.UpdateDeviceState = &InputProvider::UpdateDeviceState;
    inputProvider.HandleEvent = &InputProvider::HandleEvent;
    
    m_InputInterface->RegisterInputProvider(handle, &inputProvider);
    
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput::Start(UnitySubsystemHandle handle, void* lifecycleProviderPtr)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(lifecycleProviderPtr);
    return lifecycleProvider->StartImpl(handle);
}

UnitySubsystemErrorCode LifecycleProviderInput::StartImpl(UnitySubsystemHandle handle)
{
    m_InputInterface->InputSubsystem_DeviceConnected(handle, 0);    
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderInput::Stop(UnitySubsystemHandle handle, void* lifecycleProviderPtr)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(lifecycleProviderPtr);
    return lifecycleProvider->StopImpl(handle);
}

void LifecycleProviderInput::StopImpl(UnitySubsystemHandle handle)
{
    m_InputInterface->InputSubsystem_DeviceDisconnected(handle, 0);
}

void UNITY_INTERFACE_API LifecycleProviderInput::Shutdown(UnitySubsystemHandle handle, void* lifecycleProviderPtr)
{
    LifecycleProviderInput* lifecycleProvider = static_cast<LifecycleProviderInput*>(lifecycleProviderPtr);
    return lifecycleProvider->ShutdownImpl(handle);
}

void LifecycleProviderInput::ShutdownImpl(UnitySubsystemHandle handle)
{}