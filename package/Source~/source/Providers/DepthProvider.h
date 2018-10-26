#pragma once

#include "Unity/IUnityXRDepth.deprecated.h"

class DepthProvider : public IUnityXRDepthProvider
{
public:
    DepthProvider(IUnityXRDepthInterface*& unityInterface);

    virtual bool UNITY_INTERFACE_API GetPointCloud(IUnityXRDepthDataAllocator& xrAllocator) override;
	void PopulateCStyleProvider(UnityXRDepthProvider& xrProvider);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetPointCloud(UnitySubsystemHandle handle, void* userData, const UnityXRDepthDataAllocator* xrAllocator);
    IUnityXRDepthInterface*& m_UnityInterface;
};
