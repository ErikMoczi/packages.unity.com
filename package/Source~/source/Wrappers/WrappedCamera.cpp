#include "MathConversion.h"
#include "Unity/UnityXRTypes.h"
#include "Utility.h"
#include "WrappedCamera.h"
#include "WrappedPose.h"

template<>
void WrappingBase<ArCamera>::ReleaseImpl()
{
    ArCamera_release(m_Ptr);
}

void WrappedCamera::AcquireFromFrame()
{
    ArFrame_acquireCamera(GetArSession(), GetArFrame(), ReleaseAndGetAddressOf());
    InitRefCount();
}

void WrappedCamera::GetPose(UnityXRPose& pose) const
{
    WrappedPose wrappedPose = eWrappedConstruction::Default;
    ArCamera_getPose(GetArSession(), m_Ptr, wrappedPose);
    wrappedPose.GetXrPose(pose);
}

void WrappedCamera::GetPose(WrappedPose& pose) const
{
    ArCamera_getPose(GetArSession(), m_Ptr, pose);
}

void WrappedCamera::GetDisplayOrientedPose(UnityXRPose& pose) const
{
    WrappedPose wrappedPose = eWrappedConstruction::Default;
    ArCamera_getDisplayOrientedPose(GetArSession(), m_Ptr, wrappedPose);
    wrappedPose.GetXrPose(pose);
}

void WrappedCamera::GetDisplayOrientedPose(WrappedPose& pose) const
{
    ArCamera_getDisplayOrientedPose(GetArSession(), m_Ptr, pose);
}

void WrappedCamera::GetViewMatrix(UnityXRMatrix4x4& viewMatrix) const
{
    float rawViewMatrix[16];
    ArCamera_getViewMatrix(GetArSession(), m_Ptr, rawViewMatrix);
    MathConversion::ToUnity(viewMatrix, rawViewMatrix);
}

void WrappedCamera::GetProjectionMatrix(UnityXRMatrix4x4& projectionMatrix, float nearPlane, float farPlane) const
{
    float rawProjectionMatrix[16];
    ArCamera_getProjectionMatrix(GetArSession(), m_Ptr, nearPlane, farPlane, rawProjectionMatrix);
    MathConversion::ToUnityProjectionMatrix(projectionMatrix, rawProjectionMatrix);
}

ArTrackingState WrappedCamera::GetTrackingState() const
{
    ArTrackingState ret = AR_TRACKING_STATE_STOPPED;
    if (m_Ptr != nullptr)
        ArCamera_getTrackingState(GetArSession(), m_Ptr, &ret);
    return ret;
}
