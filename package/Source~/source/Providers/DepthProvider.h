#pragma once

#include "Unity/IUnityXRDepth.h"

class DepthProvider : public IUnityXRDepthProvider
{
public:
    virtual bool UNITY_INTERFACE_API GetPointCloud(IUnityXRDepthDataAllocator& allocator) override;
};
