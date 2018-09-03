#pragma once

#include <map>
#include <memory>

#include "Unity/IUnityXRReferencePoint.h"
#include "Utility.h"
#include "Wrappers/WrappedAnchor.h"

class ReferencePointProvider : public IUnityXRReferencePointProvider
{
public:
    virtual bool UNITY_INTERFACE_API TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& outId, UnityXRTrackingState& outTrackingState) override;
    virtual bool UNITY_INTERFACE_API TryRemoveReferencePoint(const UnityXRTrackableId& id) override;
    virtual bool UNITY_INTERFACE_API GetAllReferencePoints(IUnityXRReferencePointAllocator& allocator) override;

private:
    typedef std::map<UnityXRTrackableId, WrappedAnchor> IdToAnchorMap;
    IdToAnchorMap m_IdToAnchorMap;
};
