#pragma once

#include <memory>
#include <mutex>
#include <unordered_map>

#include "Unity/IUnityXRPlane.deprecated.h"
#include "Unity/UnityXRNativePtrs.h"
#include "Utility.h"

struct PlaneData
{
    UnityXRPlane plane;
    UnityXRTrackingState trackingState = kUnityXRTrackingStateUnknown;
    std::unique_ptr<UnityXRNativePlane> nativePlane;
};

class PlaneProvider : public IUnityXRPlaneProvider
{
public:
    static PlaneProvider* Get() { return s_Instance; }

    PlaneProvider(IUnityXRPlaneInterface*& unityInterface);
    ~PlaneProvider();

    virtual bool UNITY_INTERFACE_API GetAllPlanes(IUnityXRPlaneDataAllocator& xrAllocator) override;
    void PopulateCStyleProvider(UnityXRPlaneProvider& provider);

    UnityXRNativePlane* GetNativePlane(const UnityXRTrackableId& planeId);
    UnityXRTrackingState GetTrackingState(const UnityXRTrackableId& planeId) const;

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* xrAllocator);

    static PlaneProvider* s_Instance;

    IUnityXRPlaneInterface*& m_UnityInterface;
    int64_t m_LastFrameTimestamp;

    mutable std::mutex m_Mutex;

    std::unordered_map<UnityXRTrackableId, PlaneData, TrackableIdHasher> m_Planes;
};
