#pragma once

#include "Unity/IUnityXRRaycast.deprecated.h"

class RaycastProvider : public IUnityXRRaycastProvider
{
public:
    RaycastProvider(IUnityXRRaycastInterface*& unityInterface);

    virtual bool UNITY_INTERFACE_API Raycast(
        float screenX, float screenY,
        UnityXRTrackableType xrHitFlags,
        IUnityXRRaycastAllocator& xrAllocator) override;

    static UnitySubsystemErrorCode StaticRaycast(
        UnitySubsystemHandle handle, void* userData,
        float screenX, float screenY,
        UnityXRTrackableType xrHitFlags,
        UnityXRRaycastDataAllocator* xrAllocator);

    void PopulateCStyleProvider(UnityXRRaycastProvider& xrProvider);

private:
    IUnityXRRaycastInterface*& m_UnityInterface;
};
