#include "WrappedCameraIntrinsics.h"
#include "Utility.h"

WrappedCameraIntrinsics::WrappedCameraIntrinsics()
    : m_ArCameraIntrinsics(nullptr)
{
}

WrappedCameraIntrinsics::WrappedCameraIntrinsics(const ArCameraIntrinsics* arCameraIntrinsics)
    : m_ArCameraIntrinsics(arCameraIntrinsics)
{
}

WrappedCameraIntrinsics::operator const ArCameraIntrinsics*() const
{
    return m_ArCameraIntrinsics;
}

const ArCameraIntrinsics* WrappedCameraIntrinsics::Get() const
{
    return m_ArCameraIntrinsics;
}

void WrappedCameraIntrinsics::GetFocalLength(float& focalX, float& focalY) const
{
    ArCameraIntrinsics_getFocalLength(GetArSession(), m_ArCameraIntrinsics, &focalX, &focalY);
}

void WrappedCameraIntrinsics::GetPrincipalPoint(float& principalX, float& principalY) const
{
    ArCameraIntrinsics_getPrincipalPoint(GetArSession(), m_ArCameraIntrinsics, &principalX, &principalY);
}

void WrappedCameraIntrinsics::GetImageDimensions(size_t& width, size_t& height) const
{
    int32_t widthSigned = 0.0f;
    int32_t heightSigned = 0.0f;
    ArCameraIntrinsics_getImageDimensions(GetArSession(), m_ArCameraIntrinsics, &widthSigned, &heightSigned);

    width = widthSigned;
    height = heightSigned;
}

WrappedCameraIntrinsicsMutable::WrappedCameraIntrinsicsMutable()
{
}

WrappedCameraIntrinsicsMutable::WrappedCameraIntrinsicsMutable(ArCameraIntrinsics* arCameraIntrinsics)
    : WrappedCameraIntrinsics(arCameraIntrinsics)
{
}

WrappedCameraIntrinsicsMutable::operator ArCameraIntrinsics*()
{
    return GetArCameraIntrinsicsMutable();
}

ArCameraIntrinsics* WrappedCameraIntrinsicsMutable::Get()
{
    return GetArCameraIntrinsicsMutable();
}

void WrappedCameraIntrinsicsMutable::GetFromCameraImage()
{
    ArCamera_getImageIntrinsics(GetArSession(), GetArCamera(), GetArCameraIntrinsicsMutable());
}

void WrappedCameraIntrinsicsMutable::GetFromCameraTexture()
{
    ArCamera_getTextureIntrinsics(GetArSession(), GetArCamera(), GetArCameraIntrinsicsMutable());
}

ArCameraIntrinsics*& WrappedCameraIntrinsicsMutable::GetArCameraIntrinsicsMutable()
{
    return *const_cast<ArCameraIntrinsics**>(&m_ArCameraIntrinsics);
}

WrappedCameraIntrinsicsRaii::WrappedCameraIntrinsicsRaii()
{
    ArCameraIntrinsics_create(GetArSession(), &GetArCameraIntrinsicsMutable());
}

WrappedCameraIntrinsicsRaii::~WrappedCameraIntrinsicsRaii()
{
    if (m_ArCameraIntrinsics != nullptr)
        ArCameraIntrinsics_destroy(GetArCameraIntrinsicsMutable());
}
