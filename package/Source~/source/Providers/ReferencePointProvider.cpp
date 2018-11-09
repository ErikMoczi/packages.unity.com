#include "ReferencePointProvider.h"
#include "RemoveAbsentKeys.h"
#include "Wrappers/WrappedAnchorList.h"
#include "Unity/UnityXRNativePtrs.h"

static const UnityXRTrackableId k_InvalidId = {};
static ReferencePointProvider* s_ReferencePointProvider = nullptr;

extern "C" void* UnityARCore_getNativeReferencePointPtr(UnityXRTrackableId referencePointId)
{
    if (s_ReferencePointProvider)
        return s_ReferencePointProvider->GetNativeReferencePoint(referencePointId);

    return nullptr;
}

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

void ReferencePointProvider::AssumeOwnership(const UnityXRTrackableId& id, ArAnchor* anchor)
{
    auto nativeReferencePoint = std::unique_ptr<UnityXRNativeReferencePoint>(new UnityXRNativeReferencePoint);
    nativeReferencePoint->referencePointPtr = anchor;
    nativeReferencePoint->version = kUnityXRNativeReferencePointVersion;

    ReferencePointData data = {};
    data.arAnchor = anchor;
    data.nativeReferencePoint = std::move(nativeReferencePoint);

    m_ReferencePoints.emplace(id, std::move(data));
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

    std::lock_guard<std::mutex> lock(m_Mutex);
    AssumeOwnership(newReferencePointId, wrappedAnchor.TransferOwnership());

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

    std::lock_guard<std::mutex> lock(m_Mutex);
    AssumeOwnership(xrIdOut, wrappedAnchor.TransferOwnership());
    return true;
}

bool UNITY_INTERFACE_API ReferencePointProvider::TryRemoveReferencePoint(const UnityXRTrackableId& xrId)
{
    if (GetArSession() == nullptr)
        return false;

    std::lock_guard<std::mutex> lock(m_Mutex);

    auto iter = m_ReferencePoints.find(xrId);
    if (iter == m_ReferencePoints.end())
        return false;

    WrappedAnchorMutable wrappedAnchor = iter->second.arAnchor;
    wrappedAnchor.Detach();

    m_ReferencePoints.erase(iter);
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
    std::unordered_set<UnityXRTrackableId, TrackableIdHasher> currentReferencePoints;
    std::lock_guard<std::mutex> lock(m_Mutex);
    for (int32_t anchorIndex = 0; anchorIndex < numAnchors; ++anchorIndex)
    {
        WrappedAnchorRaii wrappedAnchor;
        wrappedAnchor.AcquireFromList(wrappedAnchorList, anchorIndex);

        UnityXRTrackableId referencePointId;
        ConvertToTrackableId(referencePointId, wrappedAnchor.Get());

        xrPoints[anchorIndex].trackingState = wrappedAnchor.GetTrackingState();
        xrPoints[anchorIndex].id = referencePointId;
        wrappedAnchor.GetPose(xrPoints[anchorIndex].pose);

        if (m_ReferencePoints.find(referencePointId) == m_ReferencePoints.end())
            AssumeOwnership(referencePointId, wrappedAnchor.TransferOwnership());

        currentReferencePoints.insert(referencePointId);
    }

    RemoveAbsentKeys(currentReferencePoints, &m_ReferencePoints);

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

UnityXRNativeReferencePoint* ReferencePointProvider::GetNativeReferencePoint(
    const UnityXRTrackableId& referencePointId)
{
    std::lock_guard<std::mutex> lock(m_Mutex);

    auto iter = m_ReferencePoints.find(referencePointId);
    if (iter != m_ReferencePoints.end())
        return iter->second.nativeReferencePoint.get();

    return nullptr;
}
