#include "ReferencePointProvider.h"
#include "Wrappers/WrappedAnchorList.h"

static const UnityXRTrackableId k_InvalidId = {};
static ReferencePointProvider* s_ReferencePointProvider = nullptr;

extern "C" UnityXRTrackableId UnityARCore_attachReferencePoint(UnityXRTrackableId trackableId, UnityXRPose pose)
{
    if (s_ReferencePointProvider == nullptr)
        return k_InvalidId;

    return s_ReferencePointProvider->AttachReferencePoint(trackableId, pose);
}

ReferencePointProvider::ReferencePointProvider(IUnityXRReferencePointInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
{
    s_ReferencePointProvider = this;    
}

ReferencePointProvider::~ReferencePointProvider()
{
    s_ReferencePointProvider = nullptr;
}

UnityXRTrackableId ReferencePointProvider::AttachReferencePoint(const UnityXRTrackableId& xrTrackableId, const UnityXRPose& xrPose)
{
    if (GetArSession() == nullptr)
        return k_InvalidId;

    auto arTrackable = ConvertTrackableIdToPtr<ArTrackable>(xrTrackableId);
    if (arTrackable == nullptr)
        return k_InvalidId;

    WrappedAnchorRaii wrappedAnchor;
    auto status = wrappedAnchor.TryAcquireAtTrackable(arTrackable, xrPose);
    if (ARSTATUS_FAILED(status))
	    return k_InvalidId;

    UnityXRTrackableId newReferencePointId;
    ConvertToTrackableId(newReferencePointId, wrappedAnchor.Get());
    m_IdToAnchorMap[newReferencePointId] = wrappedAnchor.TransferOwnership();
    return newReferencePointId;
}

bool UNITY_INTERFACE_API ReferencePointProvider::TryAddReferencePoint(const UnityXRPose& xrPose, UnityXRTrackableId& xrIdOut, UnityXRTrackingState& xrTrackingStateOut)
{
    if (GetArSession() == nullptr)
        return false;

    WrappedAnchorRaii wrappedAnchor;
    auto status = wrappedAnchor.TryAcquireAtPose(xrPose);
    if (ARSTATUS_FAILED(status))
    {
        DEBUG_LOG_WARNING("Failed to create reference point! Error: '%s'.", PrintArStatus(status));
        return false;
    }

    ConvertToTrackableId(xrIdOut, wrappedAnchor.Get());
	xrTrackingStateOut = wrappedAnchor.GetTrackingState();

	std::pair<IdToAnchorMap::iterator, bool> inserter = m_IdToAnchorMap.insert(std::make_pair(xrIdOut, wrappedAnchor.TransferOwnership()));
	IdToAnchorMap::iterator& iter = inserter.first;
    if (!inserter.second || iter == m_IdToAnchorMap.end())
    {
        DEBUG_LOG_ERROR("Internal error - can't create entry for new reference point!");
        return false;
    }

    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::TryRemoveReferencePoint(const UnityXRTrackableId& xrId)
{
    if (GetArSession() == nullptr)
        return false;

    IdToAnchorMap::iterator iter = m_IdToAnchorMap.find(xrId);
    if (iter == m_IdToAnchorMap.end())
        return false;

	WrappedAnchorRaii wrappedAnchor;
	wrappedAnchor.AssumeOwnership(iter->second);
	wrappedAnchor.Detach();

	m_IdToAnchorMap.erase(iter);
    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::GetAllReferencePoints(IUnityXRReferencePointAllocator& xrAllocator)
{
    if (GetArSession() == nullptr)
        return false;

    WrappedAnchorListRaii wrappedAnchorList;
    wrappedAnchorList.PopulateList();

    const int32_t numAnchors = wrappedAnchorList.Size();
    UnityXRReferencePoint* xrPoints = xrAllocator.AllocateReferencePoints(static_cast<size_t>(numAnchors));
    for (int32_t anchorIndex = 0; anchorIndex < numAnchors; ++anchorIndex)
    {
        WrappedAnchorRaii wrappedAnchor;
        wrappedAnchor.AcquireFromList(wrappedAnchorList, anchorIndex);

        xrPoints[anchorIndex].trackingState = wrappedAnchor.GetTrackingState();
        ConvertToTrackableId(xrPoints[anchorIndex].id, wrappedAnchor.Get());
		wrappedAnchor.GetPose(xrPoints[anchorIndex].pose);
    }

    return true;
}

struct ReferencePointAllocatorRedirect : public IUnityXRReferencePointAllocator
{
    ReferencePointAllocatorRedirect(IUnityXRReferencePointInterface* unityInterface, UnityXRReferencePointDataAllocator* xrAllocator)
        : m_UnityInterface(unityInterface)
        , m_XrAllocator(xrAllocator)
    {}

    virtual UnityXRReferencePoint* AllocateReferencePoints(
        size_t numReferencePoints) override
    {
        return m_UnityInterface->Allocator_AllocateReferencePoints(m_XrAllocator, numReferencePoints);
    }

private:
    IUnityXRReferencePointInterface* m_UnityInterface;
    UnityXRReferencePointDataAllocator* m_XrAllocator;
};

UnitySubsystemErrorCode UNITY_INTERFACE_API ReferencePointProvider::StaticTryAddReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRPose* xrPose, UnityXRTrackableId* xrIdOut, UnityXRTrackingState* xrTrackingStateOut)
{
    ReferencePointProvider* thiz = static_cast<ReferencePointProvider*>(userData);
    if (thiz == nullptr || xrPose == nullptr || xrIdOut == nullptr || xrTrackingStateOut == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->TryAddReferencePoint(*xrPose, *xrIdOut, *xrTrackingStateOut) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API ReferencePointProvider::StaticTryRemoveReferencePoint(UnitySubsystemHandle handle, void* userData, const UnityXRTrackableId* xrId)
{
    ReferencePointProvider* thiz = static_cast<ReferencePointProvider*>(userData);
    if (thiz == nullptr || xrId == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->TryRemoveReferencePoint(*xrId) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API ReferencePointProvider::StaticGetAllReferencePoints(UnitySubsystemHandle handle, void* userData, UnityXRReferencePointDataAllocator* xrAllocator)
{
    ReferencePointProvider* thiz = static_cast<ReferencePointProvider*>(userData);
    if (thiz == nullptr || thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    ReferencePointAllocatorRedirect redirect(thiz->m_UnityInterface, xrAllocator);
    return thiz->GetAllReferencePoints(redirect) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

void ReferencePointProvider::PopulateCStyleProvider(UnityXRReferencePointProvider& xrProvider)
{
    std::memset(&xrProvider, 0, sizeof(xrProvider));
    xrProvider.userData = this;
    xrProvider.TryAddReferencePoint = &StaticTryAddReferencePoint;
    xrProvider.TryRemoveReferencePoint = &StaticTryRemoveReferencePoint;
    xrProvider.GetAllReferencePoints = &StaticGetAllReferencePoints;
}
