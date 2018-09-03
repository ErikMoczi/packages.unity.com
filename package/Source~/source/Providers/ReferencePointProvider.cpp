#include "ReferencePointProvider.h"
#include "Wrappers/WrappedAnchorList.h"
#include "Wrappers/WrappedPose.h"

bool UNITY_INTERFACE_API ReferencePointProvider::TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& outId, UnityXRTrackingState& outTrackingState)
{
    WrappedAnchor anchor;
    ArStatus ars = anchor.CreateFromPose(xrPose);
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_WARNING("Failed to create reference point! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    ConvertToTrackableId(outId, anchor.Get());
    std::pair<IdToAnchorMap::iterator, bool> inserter = m_IdToAnchorMap.insert(std::make_pair(outId, anchor));
    IdToAnchorMap::iterator& iter = inserter.first;
    if (!inserter.second || iter == m_IdToAnchorMap.end())
    {
        DEBUG_LOG_ERROR("Internal error - can't create entry for new reference point!");
        return false;
    }

    ArTrackingState arTrackingState = anchor.GetTrackingState();
    outTrackingState = ConvertGoogleTrackingStateToUnity(arTrackingState);
    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::TryRemoveReferencePoint(const UnityXRTrackableId& id)
{
    IdToAnchorMap::iterator iter = m_IdToAnchorMap.find(id);
    if (iter == m_IdToAnchorMap.end())
        return false;

    iter->second.RemoveFromSessionAndRelease();
    m_IdToAnchorMap.erase(iter);
    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::GetAllReferencePoints(IUnityXRReferencePointAllocator& allocator)
{
    WrappedAnchorList anchorList = eWrappedConstruction::Default;
    anchorList.GetAllAnchors();

    int32_t numAnchors = anchorList.Size();
    UnityXRReferencePoint* unityPoints = allocator.AllocateReferencePoints(static_cast<size_t>(numAnchors));
    for (int32_t anchorIndex = 0; anchorIndex < numAnchors; ++anchorIndex)
    {
        WrappedAnchor anchor;
        anchorList.AcquireAt(anchorIndex, anchor);

        ConvertToTrackableId(unityPoints[anchorIndex].id, anchor.Get());

        ArTrackingState arTrackingState = anchor.GetTrackingState();
        unityPoints[anchorIndex].trackingState = ConvertGoogleTrackingStateToUnity(arTrackingState);

        WrappedPose pose;
        anchor.GetPose(pose);
        pose.GetXrPose(unityPoints[anchorIndex].pose);
    }

    return true;
}
