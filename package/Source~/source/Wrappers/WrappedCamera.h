#pragma once

#include "arcore_c_api.h"
#include "WrappingBase.h"

class WrappedPose;

struct UnityXRMatrix4x4;
struct UnityXRPose;

class WrappedCamera : public WrappingBase<ArCamera>
{
public:
    void AcquireFromFrame();

    void GetPose(UnityXRPose& pose) const;
    void GetPose(WrappedPose& pose) const;
    void GetDisplayOrientedPose(UnityXRPose& pose) const;
    void GetDisplayOrientedPose(WrappedPose& pose) const;

    void GetViewMatrix(UnityXRMatrix4x4& viewMatrix) const;
    void GetProjectionMatrix(UnityXRMatrix4x4& projectionMatrix, float nearPlane, float farPlane) const;

    ArTrackingState GetTrackingState() const;
};
