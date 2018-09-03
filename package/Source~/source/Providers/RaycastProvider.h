#pragma once

#include "Unity/IUnityXRRaycast.h"

class RaycastProvider : public IUnityXRRaycastProvider
{
public:
    virtual bool UNITY_INTERFACE_API Raycast(
        float screenX, float screenY,
        UnityXRTrackableType hitFlags,
        IUnityXRRaycastAllocator& allocator) override;
};
