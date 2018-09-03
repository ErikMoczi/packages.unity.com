#pragma once

#include "Unity/IUnityXRInput.h"

class InputProvider : public IUnityXRInputProvider
{
public:
    InputProvider();

    virtual void UNITY_INTERFACE_API FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition) override;
    virtual bool UNITY_INTERFACE_API UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState) override;

private:
    unsigned int m_PoseFeatureIndex;
};
