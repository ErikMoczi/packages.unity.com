#include "InputProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

InputProvider::InputProvider()
    : m_PoseFeatureIndex(static_cast<unsigned int>(-1))
{
}

void UNITY_INTERFACE_API InputProvider::FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceDefinition* const deviceDefinition)
{
    deviceDefinition->SetName("ARCore");
    deviceDefinition->SetRole(kUnityXRInputDeviceRoleGeneric);
    m_PoseFeatureIndex = deviceDefinition->AddFeature("Pose", kUnityXRInputFeatureUsageColorCameraPose, kUnityXRInputFeatureTypePose);
}

bool UNITY_INTERFACE_API InputProvider::UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, IUnityXRInputDeviceState* const deviceState)
{
    const WrappedCamera& wrappedCamera = GetWrappedCamera();
    if (wrappedCamera.GetTrackingState() != AR_TRACKING_STATE_TRACKING)
        return false;

    UnityXRPose cameraPose;
    wrappedCamera.GetDisplayOrientedPose(cameraPose);

    deviceState->SetPoseValue(m_PoseFeatureIndex, cameraPose);
    return true;
}
