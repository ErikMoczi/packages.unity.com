#include "PlaneProvider.h"
#include "RemoveAbsentKeys.h"
#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPlane.h"
#include "Wrappers/WrappedTrackable.h"
#include "Wrappers/WrappedTrackableList.h"
#include "Unity/UnityXRNativePtrs.h"

#include <algorithm>
#include <cstring>
#include <mutex>
#include <set>
#include <unordered_map>

PlaneProvider* PlaneProvider::s_Instance = nullptr;

extern "C" UnityXRTrackingState UnityARCore_getAnchorTrackingState(UnityXRTrackableId id)
{
    if (auto provider = PlaneProvider::Get())
        return provider->GetTrackingState(id);

    return kUnityXRTrackingStateUnknown;
}

extern "C" void* UnityARCore_getNativePlanePtr(UnityXRTrackableId planeId)
{
    if (auto provider = PlaneProvider::Get())
        return provider->GetNativePlane(planeId);

    return nullptr;
}

PlaneProvider::PlaneProvider(IUnityXRPlaneInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
    , m_LastFrameTimestamp(0)
{
    s_Instance = this;
}

PlaneProvider::~PlaneProvider()
{
    s_Instance = nullptr;
}

UnityXRTrackingState PlaneProvider::GetTrackingState(const UnityXRTrackableId& planeId) const
{
    if (!IsArSessionEnabled())
        return kUnityXRTrackingStateUnavailable;

    std::lock_guard<std::mutex> lock(m_Mutex);

    auto iter = m_Planes.find(planeId);
    if (iter == m_Planes.end())
        return kUnityXRTrackingStateUnknown;

    return iter->second.trackingState;
}

UnityXRNativePlane* PlaneProvider::GetNativePlane(const UnityXRTrackableId& planeId)
{
    std::lock_guard<std::mutex> lock(m_Mutex);

    auto iter = m_Planes.find(planeId);
    if (iter != m_Planes.end())
        return iter->second.nativePlane.get();

    return nullptr;
}

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API PlaneProvider::GetAllPlanes(IUnityXRPlaneDataAllocator& xrAllocator)
{
    if (GetArSession() == nullptr || GetArFrame() == nullptr)
        return false;

    int64_t latestFrameTimestamp = GetLatestTimestamp();
    if (latestFrameTimestamp == m_LastFrameTimestamp)
        return false;
	
    std::set<UnityXRTrackableId> updatedIds;
    {
        WrappedTrackableListRaii updatedPlanes;
        updatedPlanes.PopulateList_UpdatedOnly(AR_TRACKABLE_PLANE);

        const int32_t numUpdatedPlanes = updatedPlanes.Size();
        for (int32_t planeIndex = 0; planeIndex < numUpdatedPlanes; ++planeIndex)
        {
            WrappedTrackableRaii wrappedTrackable;
            wrappedTrackable.AcquireFromList(updatedPlanes, planeIndex);

            UnityXRTrackableId id;
            ConvertToTrackableId(id, wrappedTrackable.Get());
            updatedIds.insert(id);
        }
    }

    WrappedTrackableListRaii detectedPlanes;
    detectedPlanes.PopulateList_All(AR_TRACKABLE_PLANE);

    std::unordered_set<UnityXRTrackableId, TrackableIdHasher> currentPlanes;

    std::lock_guard<std::mutex> lock(m_Mutex);

    const int32_t numPlanes = detectedPlanes.Size();
    UnityXRPlane* currentXrPlane = xrAllocator.AllocatePlaneData(numPlanes);
    for (int32_t planeIndex = 0; planeIndex < numPlanes; ++planeIndex, ++currentXrPlane)
    {
        WrappedTrackableRaii wrappedTrackable;
        wrappedTrackable.AcquireFromList(detectedPlanes, planeIndex);

        WrappedPlane wrappedPlane = ArAsPlane(wrappedTrackable);
        wrappedPlane.ConvertToXRPlane(*currentXrPlane, xrAllocator);
        const auto& planeId = currentXrPlane->id;
        currentXrPlane->wasUpdated = updatedIds.find(planeId) != updatedIds.end();

        auto& planeData = m_Planes[planeId];

        auto xrTrackingState = wrappedTrackable.GetTrackingState();
        currentXrPlane->wasUpdated = currentXrPlane->wasUpdated || (planeData.trackingState != xrTrackingState);
        planeData.plane = *currentXrPlane;
        planeData.trackingState = xrTrackingState;
        if (planeData.nativePlane == nullptr)
        {
            auto nativePlane = std::unique_ptr<UnityXRNativePlane>(new UnityXRNativePlane);
            nativePlane->version = kUnityXRNativePlaneVersion;
            nativePlane->planePtr = ConvertTrackableIdToPtr<void*>(planeId);
            planeData.nativePlane = std::move(nativePlane);
        }

        currentPlanes.insert(planeId);
    }

    RemoveAbsentKeys(currentPlanes, &m_Planes);

    m_LastFrameTimestamp = latestFrameTimestamp;
    return true;
}

void PlaneProvider::PopulateCStyleProvider(UnityXRPlaneProvider& provider)
{
    std::memset(&provider, 0, sizeof(provider));
    provider.userData = this;
    provider.GetAllPlanes = &StaticGetAllPlanes;
}

struct PlaneDataAllocatorRedirect : public IUnityXRPlaneDataAllocator
{
    PlaneDataAllocatorRedirect(IUnityXRPlaneInterface* unityInterface, UnityXRPlaneDataAllocator* xrAllocator)
        : m_UnityInterface(unityInterface)
        , m_XrAllocator(xrAllocator)
    {}

    virtual UnityXRPlane* AllocatePlaneData(
        size_t numPlanes) override
    {
        return m_UnityInterface->Allocator_AllocatePlaneData(m_XrAllocator, numPlanes);
    }

    virtual UnityXRVector3* AllocateBoundaryPoints(
        const UnityXRTrackableId& xrPlaneId,
        size_t numPoints) override
    {
        return m_UnityInterface->Allocator_AllocateBoundaryPoints(m_XrAllocator, &xrPlaneId, numPoints);
    }

private:
    IUnityXRPlaneInterface* m_UnityInterface;
    UnityXRPlaneDataAllocator* m_XrAllocator;
};

UnitySubsystemErrorCode UNITY_INTERFACE_API PlaneProvider::StaticGetAllPlanes(UnitySubsystemHandle handle, void* userData, UnityXRPlaneDataAllocator* xrAllocator)
{
    PlaneProvider* thiz = static_cast<PlaneProvider*>(userData);
    if (thiz == nullptr || xrAllocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    PlaneDataAllocatorRedirect redirect(thiz->m_UnityInterface, xrAllocator);
    return thiz->GetAllPlanes(redirect) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}
