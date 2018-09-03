#include "ReferencePointProvider.h"
#include "Wrappers/WrappedAnchorList.h"
#include "Wrappers/WrappedPose.h"

static const UnityXRTrackableId s_InvalidId = {};
ReferencePointProvider* ReferencePointProvider::s_Instance = nullptr;

extern "C" UnityXRTrackableId UnityARCore_attachReferencePoint(UnityXRTrackableId trackableId, UnityXRPose pose)
{
    auto instance = ReferencePointProvider::Get();
    if (instance == nullptr)
        return s_InvalidId;

    return instance->AttachReferencePoint(trackableId, pose);
}

ReferencePointProvider::ReferencePointProvider()
{
    s_Instance = this;    
}

ReferencePointProvider::~ReferencePointProvider()
{
    s_Instance = nullptr;
}

UnityXRTrackableId ReferencePointProvider::AttachReferencePoint(UnityXRTrackableId trackableId, UnityXRPose pose)
{
    auto session = GetArSession();
    if (session == nullptr)
        return s_InvalidId;

    auto trackable = ConvertTrackableIdToPtr<ArTrackable>(trackableId);
    if (trackable == nullptr)
        return s_InvalidId;

    const float poseRaw[7] =
    {
        pose.rotation.x,
        pose.rotation.y,
        -pose.rotation.z,
        -pose.rotation.w,
        pose.position.x,
        pose.position.y,
        -pose.position.z
    };

    ArPose* arPose = nullptr;
    ArPose_create(session, poseRaw, &arPose);

    ArAnchor* anchor = nullptr;
    auto status = ArTrackable_acquireNewAnchor(session, trackable, arPose, &anchor);
    ArPose_destroy(arPose);

    if (ARSTATUS_FAILED(status))
        return s_InvalidId;

    UnityXRTrackableId newReferencePointId;
    ConvertToTrackableId(newReferencePointId, anchor);

    WrappedAnchor wrappedAnchor;
    wrappedAnchor.AssumeOwnership(anchor);

    m_IdToAnchorMap[newReferencePointId] = wrappedAnchor;

    return newReferencePointId;
}

bool UNITY_INTERFACE_API ReferencePointProvider::TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& outId, UnityXRTrackingState& outTrackingState)
{
    if (GetArSession() == nullptr)
        return false;

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
    if (GetArSession() == nullptr)
        return false;

    IdToAnchorMap::iterator iter = m_IdToAnchorMap.find(id);
    if (iter == m_IdToAnchorMap.end())
        return false;

    iter->second.RemoveFromSessionAndRelease();
    m_IdToAnchorMap.erase(iter);
    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::GetAllReferencePoints(IUnityXRReferencePointAllocator& allocator)
{
    if (GetArSession() == nullptr)
        return false;

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
