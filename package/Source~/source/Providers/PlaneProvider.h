#pragma once

#include "Unity/IUnityXRPlane.h"

class PlaneProvider : public IUnityXRPlaneProvider
{
public:
    virtual bool UNITY_INTERFACE_API GetAllPlanes(IUnityXRPlaneDataAllocator& allocator) override;

private:
    int64_t m_LastFrameTimestamp = 0;
};
