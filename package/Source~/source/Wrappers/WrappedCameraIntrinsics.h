#pragma once

#include "arcore_c_api.h"

class WrappedCameraIntrinsics
{
public:
    WrappedCameraIntrinsics();
    WrappedCameraIntrinsics(const ArCameraIntrinsics* arCameraIntrinsics);

    operator const ArCameraIntrinsics*() const;
    const ArCameraIntrinsics* Get() const;

    void GetFocalLength(float& focalX, float& focalY) const;
    void GetPrincipalPoint(float& principalX, float& principalY) const;
    void GetImageDimensions(size_t& width, size_t& height) const;

protected:
    const ArCameraIntrinsics* m_ArCameraIntrinsics;
};

class WrappedCameraIntrinsicsMutable : public WrappedCameraIntrinsics
{
public:
    WrappedCameraIntrinsicsMutable();
    WrappedCameraIntrinsicsMutable(ArCameraIntrinsics* arCameraIntrinsics);

    operator ArCameraIntrinsics*();
    ArCameraIntrinsics* Get();

    void GetFromCameraImage();
    void GetFromCameraTexture();

protected:
    ArCameraIntrinsics*& GetArCameraIntrinsicsMutable();
};

class WrappedCameraIntrinsicsRaii : public WrappedCameraIntrinsicsMutable
{
public:
    WrappedCameraIntrinsicsRaii();
    ~WrappedCameraIntrinsicsRaii();

private:
    WrappedCameraIntrinsicsRaii(const WrappedCameraIntrinsicsRaii&);
    WrappedCameraIntrinsicsRaii& operator=(const WrappedCameraIntrinsicsRaii&);
};
