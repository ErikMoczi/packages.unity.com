#pragma once

#include "arcore_c_api.h"
#include "Unity/UnityXRTrackable.h"

struct UnityXRMatrix4x4;
struct UnityXRPose;

class WrappedCamera
{
public:
    WrappedCamera();
    WrappedCamera(const ArCamera* arCamera);

    operator const ArCamera*() const;
    const ArCamera* Get() const;

    void GetPose(ArPose* arPose) const;
    void GetPose(UnityXRPose& xrPose) const;
    void GetDisplayOrientedPose(ArPose* arPose) const;
    void GetDisplayOrientedPose(UnityXRPose& xrPose) const;

    void GetViewMatrix(UnityXRMatrix4x4& xrViewMatrix) const;
    void GetProjectionMatrix(UnityXRMatrix4x4& xrProjectionMatrix, float nearPlane, float farPlane) const;

    UnityXRTrackingState GetTrackingState() const;

protected:
    const ArCamera* m_ArCamera;
};

class WrappedCameraMutable : public WrappedCamera
{
public:
    WrappedCameraMutable();
    WrappedCameraMutable(ArCamera* arCamera);

    operator ArCamera*();
    ArCamera* Get();

protected:
    ArCamera*& GetArCameraMutable();
};

class WrappedCameraRaii : public WrappedCameraMutable
{
public:
    WrappedCameraRaii();
    ~WrappedCameraRaii();

    void AcquireFromFrame();
    void Release();

private:
    WrappedCameraRaii(const WrappedCameraRaii& original);
    WrappedCameraRaii& operator=(const WrappedCameraRaii& copyFrom);
};
