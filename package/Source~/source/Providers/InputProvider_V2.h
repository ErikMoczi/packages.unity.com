#pragma once

#include "Unity/IUnityXRInput_V2.h"

class InputProvider_V2 : public UnityXRInput_V2::IUnityXRInputProvider
{
public:
    InputProvider_V2();

    virtual void UNITY_INTERFACE_API FillDeviceDefinition(UnityXRInput_V2::UnityXRInternalInputDeviceId deviceId, UnityXRInput_V2::IUnityXRInputDeviceDefinition* const deviceDefinition) override;
    virtual bool UNITY_INTERFACE_API UpdateDeviceState(UnityXRInput_V2::UnityXRInternalInputDeviceId deviceId, UnityXRInput_V2::IUnityXRInputDeviceState* const deviceState) override;
    virtual bool UNITY_INTERFACE_API HandleEvent(UnityXRInput_V2::UnityXRInputEventType eventType, UnityXRInput_V2::UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size);

private:
    unsigned int m_PositionFeatureIndex;
    unsigned int m_RotationFeatureIndex;
};
