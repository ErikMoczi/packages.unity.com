#include "InputProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

InputProvider::InputProvider()
    : m_PositionFeatureIndex(static_cast<unsigned int>(-1))
    , m_RotationFeatureIndex(static_cast<unsigned int>(-1))
{}

void InputProvider::SetInputInterface(IUnityXRInputInterface* inputInterface)
{
    m_InputInterface = inputInterface;
}

void UNITY_INTERFACE_API InputProvider::FillDeviceDefinition(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle)
{
    InputProvider* inputProvider = static_cast<InputProvider*>(inputProviderPtr);
    inputProvider->FillDeviceDefinitionImpl(deviceId, definitionHandle);
}

void InputProvider::FillDeviceDefinitionImpl(UnityXRInternalInputDeviceId deviceId, UnityXRInputDeviceDefinitionHandle definitionHandle)
{
    if(nullptr == m_InputInterface)
        return;
    
    m_InputInterface->DeviceDefinition_SetName(definitionHandle, "ARCore");
    m_InputInterface->DeviceDefinition_SetRole(definitionHandle, kUnityXRInputDeviceRoleGeneric);
    
    m_PositionFeatureIndex = m_InputInterface->DeviceDefinition_AddFeatureWithUsage(definitionHandle, "Device - Position", kUnityXRInputFeatureTypeAxis3D, kUnityXRInputFeatureUsageColorCameraPosition);
    m_RotationFeatureIndex = m_InputInterface->DeviceDefinition_AddFeatureWithUsage(definitionHandle, "Device - Rotation", kUnityXRInputFeatureTypeRotation, kUnityXRInputFeatureUsageColorCameraRotation);  
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::UpdateDeviceState(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle)
{
    InputProvider* inputProvider = static_cast<InputProvider*>(inputProviderPtr);
    return inputProvider->UpdateDeviceStateImpl(deviceId, updateType, stateHandle);
}

UnitySubsystemErrorCode InputProvider::UpdateDeviceStateImpl(UnityXRInternalInputDeviceId deviceId, UnityXRInputUpdateType updateType, UnityXRInputDeviceStateHandle stateHandle)
{
    if(nullptr == m_InputInterface)
        return kUnitySubsystemErrorCodeFailure;

    const WrappedCamera& wrappedCamera = GetWrappedCamera();
    if (wrappedCamera.GetTrackingState() != AR_TRACKING_STATE_TRACKING)
        return kUnitySubsystemErrorCodeFailure;

    UnityXRPose cameraPose;
    wrappedCamera.GetDisplayOrientedPose(cameraPose);

    m_InputInterface->DeviceState_SetAxis3DValue(stateHandle, m_PositionFeatureIndex, cameraPose.position);
    m_InputInterface->DeviceState_SetRotationValue(stateHandle, m_RotationFeatureIndex, cameraPose.rotation);
    return kUnitySubsystemErrorCodeSuccess;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::HandleEvent(UnitySubsystemHandle handle, void* inputProviderPtr, UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    InputProvider* inputProvider = static_cast<InputProvider*>(inputProviderPtr);
    return inputProvider->HandleEventImpl(eventType, deviceId, buffer, size);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API InputProvider::HandleEventImpl(UnityXRInputEventType eventType, UnityXRInternalInputDeviceId deviceId, void* buffer, unsigned int size)
{
    return kUnitySubsystemErrorCodeFailure;
}

