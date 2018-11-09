#pragma once

#include <memory>
#include <mutex>
#include <unordered_map>

#include "Unity/IUnityXRReferencePoint.deprecated.h"
#include "Utility.h"
#include "Wrappers/WrappedAnchor.h"
#include "Unity/UnityXRNativePtrs.h"

struct ReferencePointData
{
    ArAnchor* arAnchor;
    std::unique_ptr<UnityXRNativeReferencePoint> nativeReferencePoint;

    ReferencePointData() = default;
    ReferencePointData(const ReferencePointData&) = delete;
    ReferencePointData(ReferencePointData&& other)
    : arAnchor(other.arAnchor)
    , nativeReferencePoint(std::move(other.nativeReferencePoint))
    {
        other.arAnchor = nullptr;
    }

    ~ReferencePointData()
    {
        if (arAnchor)
            ArAnchor_release(arAnchor);
    }
};

class ReferencePointProvider : public IUnityXRReferencePointProvider
{
public:
    ReferencePointProvider(IUnityXRReferencePointInterface*& unityInterface);
    virtual ~ReferencePointProvider();
    virtual bool UNITY_INTERFACE_API TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& xrIdOut, UnityXRTrackingState& xrTrackingStateOut) override;
    virtual bool UNITY_INTERFACE_API TryRemoveReferencePoint(const UnityXRTrackableId& xrId) override;
    virtual bool UNITY_INTERFACE_API GetAllReferencePoints(IUnityXRReferencePointAllocator& xrAllocator) override;

    UnityXRTrackableId AttachReferencePoint(const UnityXRTrackableId& xrTrackableId, const UnityXRPose& xrPose);
    void PopulateCStyleProvider(UnityXRReferencePointProvider& xrProvider);

    UnityXRNativeReferencePoint* GetNativeReferencePoint(const UnityXRTrackableId& referencePointId);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryAddReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRPose* xrPose, UnityXRTrackableId* xrIdOut, UnityXRTrackingState* xrTrackingStateOut);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryRemoveReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRTrackableId* xrId);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllReferencePoints(UnitySubsystemHandle handle, void* userData, UnityXRReferencePointDataAllocator* xrAllocator);

    // Add the ArAnchor to our map and release it when done
    void AssumeOwnership(const UnityXRTrackableId& id, ArAnchor* anchor);

    IUnityXRReferencePointInterface*& m_UnityInterface;

    std::mutex m_Mutex;

    std::unordered_map<UnityXRTrackableId, ReferencePointData, TrackableIdHasher> m_ReferencePoints;
};
