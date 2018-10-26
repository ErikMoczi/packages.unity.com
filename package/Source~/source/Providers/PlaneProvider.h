#pragma once

#include "Unity/IUnityXRPlane.deprecated.h"

class PlaneProvider : public IUnityXRPlaneProvider
{
public:
    PlaneProvider(IUnityXRPlaneInterface*& unityInterface);

    virtual bool UNITY_INTERFACE_API GetAllPlanes(IUnityXRPlaneDataAllocator& xrAllocator) override;
    void PopulateCStyleProvider(UnityXRPlaneProvider& provider);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* xrAllocator);

    IUnityXRPlaneInterface*& m_UnityInterface;
    int64_t m_LastFrameTimestamp;
};
