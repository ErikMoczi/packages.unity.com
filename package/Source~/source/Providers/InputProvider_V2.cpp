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
    WrappedCamera wrappedCamera = GetArCamera();
    if (wrappedCamera == nullptr)
        return false;

    if (wrappedCamera.GetTrackingState() != kUnityXRTrackingStateTracking)
        return false;

    UnityXRPose xrCameraPose;
    wrappedCamera.GetDisplayOrientedPose(xrCameraPose);

    deviceState->SetAxis3DValue(m_PositionFeatureIndex, xrCameraPose.position);
    deviceState->SetRotationValue(m_RotationFeatureIndex, xrCameraPose.rotation);
    return true;
}

bool UNITY_INTERFACE_API InputProvider_V2::HandleEvent(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    return false;
}
