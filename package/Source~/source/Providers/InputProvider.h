#pragma once

#include "Unity/IUnityXRInput.h"

class InputProvider
{
public:
    InputProvider();

    static void UNITY_INTERFACE_API OnNewInputFrame(UnitySubsystemHandle handle , void* inputProviderPtr) {}
    static void UNITY_INTERFACE_API FillDeviceDefinition(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API UpdateDeviceState(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API HandleEvent(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);

    void SetInputInterface(IUnityXRInputInterface* inputInterface);

    void FillDeviceDefinitionImpl(UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle);
    UnitySubsystemErrorCode UpdateDeviceStateImpl(UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle);
    UnitySubsystemErrorCode HandleEventImpl(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);
private:
    IUnityXRInputInterface* m_InputInterface;
    unsigned int m_PositionFeatureIndex;
    unsigned int m_RotationFeatureIndex;
};
