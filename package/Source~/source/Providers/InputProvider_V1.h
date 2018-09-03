#pragma once

#include "Unity/IUnityXRInput_V1.h"

class InputProvider_V1 : public UnityXRInput_V1::IUnityXRInputProvider
{
public:
    InputProvider_V1();

    virtual void UNITY_INTERFACE_API FillDeviceDefinition(UnityXRInput_V1::UnityXRInternalInputDeviceId deviceId, UnityXRInput_V1::IUnityXRInputDeviceDefinition* const deviceDefinition) override;
    virtual bool UNITY_INTERFACE_API UpdateDeviceState(UnityXRInput_V1::UnityXRInternalInputDeviceId deviceId, UnityXRInput_V1::IUnityXRInputDeviceState* const deviceState) override;

private:
    unsigned int m_PoseFeatureIndex;
};
