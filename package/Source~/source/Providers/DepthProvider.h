#pragma once

#include "Unity/IUnityXRDepth.deprecated.h"

class DepthProvider : public IUnityXRDepthProvider
{
public:
    DepthProvider(IUnityXRDepthInterface*& unityInterface);

    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetPointCloud(UnitySubsystemHandle handle, void* userData, const UnityXRDepthDataAllocator* allocator);
    virtual bool UNITY_INTERFACE_API GetPointCloud(IUnityXRDepthDataAllocator& allocator) override;
	void PopulateCStyleProvider(UnityXRDepthProvider& provider);

private:
    IUnityXRDepthInterface*& m_UnityInterface;
};
