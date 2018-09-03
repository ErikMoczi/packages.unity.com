#pragma once

#include "Unity/IUnityXRRaycast.deprecated.h"

class RaycastProvider : public IUnityXRRaycastProvider
{
public:
    RaycastProvider(IUnityXRRaycastInterface*& unityInterface);

    virtual bool UNITY_INTERFACE_API Raycast(
        float screenX, float screenY,
        UnityXRTrackableType hitFlags,
        IUnityXRRaycastAllocator& allocator) override;


    static UnitySubsystemErrorCode StaticRaycast(
        UnitySubsystemHandle handle, void* userData,
        float screenX, float screenY,
        UnityXRTrackableType hitFlags,
    UnityXRRaycastDataAllocator* allocator);
    void PopulateCStyleProvider(UnityXRRaycastProvider& provider);

private:
    IUnityXRRaycastInterface*& m_UnityInterface;
};
