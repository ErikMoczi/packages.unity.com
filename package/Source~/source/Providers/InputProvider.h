#pragma once

#include "Unity/IUnityXRInput.h"

class InputProvider : public IUnityXRInputProvider
{
public:
    InputProvider();

    virtual void UNITY_INTERFACE_API FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition) override;
    virtual bool UNITY_INTERFACE_API UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState) override;
    virtual bool UNITY_INTERFACE_API HandleEvent(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);

private:
    unsigned int m_PositionFeatureIndex;
    unsigned int m_RotationFeatureIndex;
};
