#include "InputProvider_V2.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

using namespace UnityXRInput_V2;

InputProvider_V2::InputProvider_V2()
    : m_PositionFeatureIndex(static_cast<unsigned int>(-1))
    , m_RotationFeatureIndex(static_cast<unsigned int>(-1))
{
}

void UNITY_INTERFACE_API InputProvider_V2::FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition)
{
    deviceDefinition->SetName("ARCore");
    deviceDefinition->SetRole(kUnityXRInputDeviceRoleGeneric);
    
    m_PositionFeatureIndex = deviceDefinition->AddFeatureWithUsage("Camera - Position", kUnityXRInputFeatureTypeAxis3D, kUnityXRInputFeatureUsageColorCameraPosition);
    m_RotationFeatureIndex = deviceDefinition->AddFeatureWithUsage("Camera - Rotation", kUnityXRInputFeatureTypeRotation, kUnityXRInputFeatureUsageColorCameraRotation);
}

bool UNITY_INTERFACE_API InputProvider_V2::UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState)
{
    const WrappedCamera& wrappedCamera = GetWrappedCamera();
    if (wrappedCamera.GetTrackingState() != AR_TRACKING_STATE_TRACKING)
        return false;

    UnityXRPose cameraPose;
    wrappedCamera.GetDisplayOrientedPose(cameraPose);

    deviceState->SetAxis3DValue(m_PositionFeatureIndex, cameraPose.position);
    deviceState->SetRotationValue(m_RotationFeatureIndex, cameraPose.rotation);
    return true;
}

bool UNITY_INTERFACE_API InputProvider_V2::HandleEvent(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    return false;
}
