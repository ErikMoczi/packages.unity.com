#include "InputProvider_V1.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

using namespace UnityXRInput_V1;

InputProvider_V1::InputProvider_V1()
    : m_PoseFeatureIndex(static_cast<unsigned int>(-1))
{
}

void UNITY_INTERFACE_API InputProvider_V1::FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition)
{
    deviceDefinition->SetName("ARCore");
    deviceDefinition->SetRole(kUnityXRInputDeviceRoleGeneric);
    m_PoseFeatureIndex = deviceDefinition->AddFeature("Pose", kUnityXRInputFeatureUsageColorCameraPose, kUnityXRInputFeatureTypePose);
}

bool UNITY_INTERFACE_API InputProvider_V1::UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState)
{
    WrappedCamera wrappedCamera = GetArCamera();
    if (wrappedCamera == nullptr)
        return false;

    if (wrappedCamera.GetTrackingState() != kUnityXRTrackingStateTracking)
        return false;

    UnityXRPose xrCameraPose;
    wrappedCamera.GetDisplayOrientedPose(xrCameraPose);

    deviceState->SetPoseValue(m_PoseFeatureIndex, xrCameraPose);
    return true;
}
