#pragma once

#include "Unity/IUnityXRPlane.deprecated.h"

class PlaneProvider : public IUnityXRPlaneProvider
{
public:
    PlaneProvider(IUnityXRPlaneInterface*& unityInterface);

    virtual bool UNITY_INTERFACE_API GetAllPlanes(IUnityXRPlaneDataAllocator& allocator) override;

    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* allocator);
    void PopulateCStyleProvider(UnityXRPlaneProvider& provider);

private:
    IUnityXRPlaneInterface*& m_UnityInterface;
    int64_t m_LastFrameTimestamp;
};
