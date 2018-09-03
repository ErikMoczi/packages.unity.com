#pragma once

#include <map>
#include <memory>

#include "Unity/IUnityXRReferencePoint.deprecated.h"
#include "Utility.h"
#include "Wrappers/WrappedAnchor.h"

class ReferencePointProvider : public IUnityXRReferencePointProvider
{
public:
    ReferencePointProvider(IUnityXRReferencePointInterface*& unityInterface);
    virtual ~ReferencePointProvider();
    virtual bool UNITY_INTERFACE_API TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& outId, UnityXRTrackingState& outTrackingState) override;
    virtual bool UNITY_INTERFACE_API TryRemoveReferencePoint(const UnityXRTrackableId& id) override;
    virtual bool UNITY_INTERFACE_API GetAllReferencePoints(IUnityXRReferencePointAllocator& allocator) override;

    static ReferencePointProvider* Get() { return s_Instance; }

    UnityXRTrackableId AttachReferencePoint(UnityXRTrackableId trackableId, UnityXRPose pose);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryAddReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRPose* xrPose, UnityXRTrackableId* outId, UnityXRTrackingState* outTrackingState);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryRemoveReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRTrackableId* id);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllReferencePoints(UnitySubsystemHandle handle, void* userData, UnityXRReferencePointDataAllocator* allocator);
    void PopulateCStyleProvider(UnityXRReferencePointProvider& provider);

private:
    IUnityXRReferencePointInterface*& m_UnityInterface;
    static ReferencePointProvider* s_Instance;

    typedef std::map<UnityXRTrackableId, WrappedAnchor> IdToAnchorMap;
    IdToAnchorMap m_IdToAnchorMap;
};
