#include "MathConversion.h"
#include "Utility.h"
#include "WrappedCamera.h"
#include "WrappedPose.h"

#include "Unity/UnityXRTypes.h"

WrappedCamera::WrappedCamera()
    : m_ArCamera(nullptr)
{
}

WrappedCamera::WrappedCamera(const ArCamera* arCamera)
    : m_ArCamera(arCamera)
{
}

WrappedCamera::operator const ArCamera*() const
{
    return m_ArCamera;
}

const ArCamera* WrappedCamera::Get() const
{
    return m_ArCamera;
}

void WrappedCamera::GetPose(ArPose* arPose) const
{
    ArCamera_getPose(GetArSession(), m_ArCamera, arPose);
}

void WrappedCamera::GetPose(UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose;
    ArCamera_getPose(GetArSession(), m_ArCamera, wrappedPose);
    wrappedPose.GetPose(xrPose);
}

void WrappedCamera::GetDisplayOrientedPose(ArPose* arPose) const
{
    ArCamera_getDisplayOrientedPose(GetArSession(), m_ArCamera, arPose);
}

void WrappedCamera::GetDisplayOrientedPose(UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose;
    GetDisplayOrientedPose(wrappedPose);
    wrappedPose.GetPose(xrPose);
}

void WrappedCamera::GetViewMatrix(UnityXRMatrix4x4& xrViewMatrix) const
{
    float googleViewMatrix[16];
    ArCamera_getViewMatrix(GetArSession(), m_ArCamera, googleViewMatrix);
    MathConversion::ToUnity(xrViewMatrix, googleViewMatrix);
}

void WrappedCamera::GetProjectionMatrix(UnityXRMatrix4x4& xrProjectionMatrix, float nearPlane, float farPlane) const
{
    float googleProjectionMatrix[16];
    ArCamera_getProjectionMatrix(GetArSession(), m_ArCamera, nearPlane, farPlane, googleProjectionMatrix);
    MathConversion::ToUnityProjectionMatrix(xrProjectionMatrix, googleProjectionMatrix);
}

UnityXRTrackingState WrappedCamera::GetTrackingState() const
{
    ArTrackingState arTrackingState = AR_TRACKING_STATE_STOPPED;
    ArCamera_getTrackingState(GetArSession(), m_ArCamera, &arTrackingState);
    return ConvertGoogleTrackingStateToUnity(arTrackingState);
}

WrappedCameraMutable::WrappedCameraMutable()
{
}

WrappedCameraMutable::WrappedCameraMutable(ArCamera* arCamera)
    : WrappedCamera(arCamera)
{
}

WrappedCameraMutable::operator ArCamera*()
{
    return GetArCameraMutable();
}

ArCamera* WrappedCameraMutable::Get()
{
    return GetArCameraMutable();
}

ArCamera*& WrappedCameraMutable::GetArCameraMutable()
{
    return *const_cast<ArCamera**>(&m_ArCamera);
}

WrappedCameraRaii::WrappedCameraRaii()
{
}

WrappedCameraRaii::~WrappedCameraRaii()
{
    Release();
}

void WrappedCameraRaii::AcquireFromFrame()
{
    Release();
    ArFrame_acquireCamera(GetArSession(), GetArFrame(), &GetArCameraMutable());
}

void WrappedCameraRaii::Release()
{
    if (m_ArCamera == nullptr)
        return;

    ArCamera_release(GetArCameraMutable());
    m_ArCamera = nullptr;
}
