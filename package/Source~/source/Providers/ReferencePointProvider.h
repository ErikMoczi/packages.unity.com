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
    virtual bool UNITY_INTERFACE_API TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& xrIdOut, UnityXRTrackingState& xrTrackingStateOut) override;
    virtual bool UNITY_INTERFACE_API TryRemoveReferencePoint(const UnityXRTrackableId& xrId) override;
    virtual bool UNITY_INTERFACE_API GetAllReferencePoints(IUnityXRReferencePointAllocator& xrAllocator) override;

    UnityXRTrackableId AttachReferencePoint(const UnityXRTrackableId& xrTrackableId, const UnityXRPose& xrPose);
    void PopulateCStyleProvider(UnityXRReferencePointProvider& xrProvider);

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryAddReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRPose* xrPose, UnityXRTrackableId* xrIdOut, UnityXRTrackingState* xrTrackingStateOut);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticTryRemoveReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRTrackableId* xrId);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetAllReferencePoints(UnitySubsystemHandle handle, void* userData, UnityXRReferencePointDataAllocator* xrAllocator);

    IUnityXRReferencePointInterface*& m_UnityInterface;

    typedef std::map<UnityXRTrackableId, ArAnchor*> IdToAnchorMap;
    IdToAnchorMap m_IdToAnchorMap;
};
