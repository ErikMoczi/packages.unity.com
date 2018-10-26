#pragma once

#include "Unity/IUnityXRInput.h"

class InputProvider
{
public:
    InputProvider();

    void SetInputInterface(IUnityXRInputInterface* inputInterface);
	void PopulateCStyleProvider(UnityXRInputProvider& xrProvider);

private:
    void FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle);
    UnitySubsystemErrorCode UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle);
    UnitySubsystemErrorCode HandleEvent(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);

    static void UNITY_INTERFACE_API StaticOnNewInputFrame(UnitySubsystemHandle handle, void* userData) {}
    static void UNITY_INTERFACE_API StaticFillDeviceDefinition(UnitySubsystemHandle handle, void* userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticUpdateDeviceState(UnitySubsystemHandle handle, void* userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticHandleEvent(UnitySubsystemHandle handle, void* userData, UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);

    IUnityXRInputInterface* m_UnityInterface;
    unsigned int m_PositionFeatureIndex;
    unsigned int m_RotationFeatureIndex;
};
