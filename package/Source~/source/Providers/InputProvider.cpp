#include "InputProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

InputProvider::InputProvider()
    : m_PositionFeatureIndex(static_cast<unsigned int>(-1))
    , m_RotationFeatureIndex(static_cast<unsigned int>(-1))
{}

void InputProvider::SetInputInterface(IUnityXRInputInterface* unityInterface)
{
    m_UnityInterface = unityInterface;
}

void InputProvider::PopulateCStyleProvider(UnityXRInputProvider& xrProvider)
{
    std::memset(&xrProvider, 0, sizeof(xrProvider));
    xrProvider.userData = this;
    xrProvider.OnNewInputFrame = &StaticOnNewInputFrame;
    xrProvider.FillDeviceDefinition = &StaticFillDeviceDefinition;
    xrProvider.UpdateDeviceState = &StaticUpdateDeviceState;
    xrProvider.HandleEvent = &StaticHandleEvent;
}

void InputProvider::FillDeviceDefinition(UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle)
{
    if (m_UnityInterface == nullptr)
        return;

    m_UnityInterface->DeviceDefinition_SetName(definitionHandle, "ARCore");
    m_UnityInterface->DeviceDefinition_SetRole(definitionHandle, kUnityXRInputDeviceRoleGeneric);

    m_PositionFeatureIndex = m_UnityInterface->DeviceDefinition_AddFeatureWithUsage(definitionHandle, "Device - Position", kUnityXRInputFeatureTypeAxis3D, kUnityXRInputFeatureUsageColorCameraPosition);
    m_RotationFeatureIndex = m_UnityInterface->DeviceDefinition_AddFeatureWithUsage(definitionHandle, "Device - Rotation", kUnityXRInputFeatureTypeRotation, kUnityXRInputFeatureUsageColorCameraRotation);  
}

UnitySubsystemErrorCode InputProvider::UpdateDeviceState(UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle)
{
    if (m_UnityInterface == nullptr)
        return kUnitySubsystemErrorCodeFailure;

    WrappedCamera wrappedCamera = GetArCamera();
    if (wrappedCamera == nullptr)
        return kUnitySubsystemErrorCodeFailure;

    if (wrappedCamera.GetTrackingState() != kUnityXRTrackingStateTracking)
        return kUnitySubsystemErrorCodeFailure;

    UnityXRPose xrCameraPose;
    wrappedCamera.GetDisplayOrientedPose(xrCameraPose);

    m_UnityInterface->DeviceState_SetAxis3DValue(stateHandle, m_PositionFeatureIndex, xrCameraPose.position);
    m_UnityInterface->DeviceState_SetRotationValue(stateHandle, m_RotationFeatureIndex, xrCameraPose.rotation);
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::HandleEvent(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    return kUnitySubsystemErrorCodeFailure;
}

void UNITY_INTERFACE_API InputProvider::StaticFillDeviceDefinition(UnitySubsystemHandle handle, void* userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle)
{
    InputProvider* thiz = static_cast<InputProvider*>(userData);
    if (thiz == nullptr)
        return;

    thiz->FillDeviceDefinition(deviceId, definitionHandle);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::StaticUpdateDeviceState(UnitySubsystemHandle handle, void* userData, UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle)
{
    InputProvider* thiz = static_cast<InputProvider*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->UpdateDeviceState(deviceId, updateType, stateHandle);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::StaticHandleEvent(UnitySubsystemHandle handle, void* userData, UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    InputProvider* thiz = static_cast<InputProvider*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->HandleEvent(eventType, deviceId, buffer, size);
}
