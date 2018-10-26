#include "RaycastProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedHitResult.h"
#include "Wrappers/WrappedHitResultList.h"
#include "Wrappers/WrappedPlane.h"
#include "Wrappers/WrappedPose.h"
#include "Wrappers/WrappedTrackable.h"

#include <cstring>
#include <vector>

RaycastProvider::RaycastProvider(IUnityXRRaycastInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
{
}

static UnityXRTrackableType GetHitType(
    WrappedTrackable wrappedTrackable,
    const ArPose* arPose,
    UnityXRTrackableType xrTrackableFlags)
{
    UnityXRTrackableType xrHitTypeFlags = kUnityXRTrackableTypeNone;
    const ArTrackableType arTrackableType = wrappedTrackable.GetType();

    switch (arTrackableType)
    {
        case AR_TRACKABLE_PLANE:
        {
            if (xrTrackableFlags & kUnityXRTrackableTypePlaneWithinInfinity)
                xrHitTypeFlags = static_cast<UnityXRTrackableType>(xrHitTypeFlags | kUnityXRTrackableTypePlaneWithinInfinity);

            ///
            // Check polygon and bounds
            //
            WrappedPlane wrappedPlane = ArAsPlane(wrappedTrackable.Get());
            if (xrTrackableFlags & kUnityXRTrackableTypePlaneWithinPolygon)
            {
                if (wrappedPlane.IsPoseInPolygon(arPose))
                    xrHitTypeFlags = static_cast<UnityXRTrackableType>(xrHitTypeFlags | kUnityXRTrackableTypePlaneWithinPolygon);
            }

            if (xrTrackableFlags & kUnityXRTrackableTypePlaneWithinBounds)
            {
                if (wrappedPlane.IsPoseInExtents(arPose))
                    xrHitTypeFlags = static_cast<UnityXRTrackableType>(xrHitTypeFlags | kUnityXRTrackableTypePlaneWithinBounds);
            }

            break;
        }

        case AR_TRACKABLE_POINT:
        {
            if (xrTrackableFlags & kUnityXRTrackableTypePoint)
                xrHitTypeFlags = static_cast<UnityXRTrackableType>(xrHitTypeFlags| kUnityXRTrackableTypePoint);
            break;
        }

        default:
            break;
    }

    return xrHitTypeFlags;
}

// TODO: return false under the right circumstances (tracking loss, at least - maybe more?)
bool UNITY_INTERFACE_API RaycastProvider::Raycast(
    float screenX, float screenY,
    UnityXRTrackableType xrHitFlags,
    IUnityXRRaycastAllocator& xrAllocator)
{
    const float screenWidth = GetScreenWidth();
    const float screenHeight = GetScreenHeight();
    if (screenWidth < 0.0f || screenHeight < 0.0f)
        return false;

    screenY = 1.0f - screenY;
    screenX *= screenWidth;
    screenY *= screenHeight;

    WrappedHitResultListRaii wrappedHitResultList;
    wrappedHitResultList.HitTest(screenX, screenY);

    std::vector<UnityXRRaycastHit> xrRaycastHits;
    xrRaycastHits.reserve(wrappedHitResultList.Size());

    const int32_t numHitResults = wrappedHitResultList.Size();
    for (int32_t hitResultIndex = 0; hitResultIndex < numHitResults; ++hitResultIndex)
    {
        UnityXRRaycastHit xrRaycastHit;

        // Get the hit result from ARCore
        WrappedHitResultRaii wrappedHitResult;
        wrappedHitResult.GetFromList(wrappedHitResultList, hitResultIndex);

        // Extract the trackable
        WrappedTrackableRaii wrappedTrackable;
        wrappedTrackable.AcquireFromHitResult(wrappedHitResult);

        // Extract the pose
        WrappedPoseRaii wrappedPose;
        wrappedHitResult.GetPose(wrappedPose);
        wrappedPose.GetPose(xrRaycastHit.pose);

        // Fill out the Unity struct
        xrRaycastHit.hitType = GetHitType(wrappedTrackable, wrappedPose, xrHitFlags);

        // Skip if it doesn't match the filter
        if (xrRaycastHit.hitType == kUnityXRTrackableTypeNone)
            continue;

        ConvertToTrackableId(xrRaycastHit.trackableId, wrappedTrackable.Get());
        xrRaycastHit.distance = wrappedHitResult.GetDistance();
        xrRaycastHits.push_back(xrRaycastHit);
    }

    if (xrRaycastHits.empty())
        return false;

    UnityXRRaycastHit* xrAllocatedResults = xrAllocator.SetNumberOfHits(xrRaycastHits.size());
    std::memcpy(xrAllocatedResults, xrRaycastHits.data(), sizeof(UnityXRRaycastHit) * xrRaycastHits.size());
    return true;
}

struct RaycastAllocatorRedirect : public IUnityXRRaycastAllocator
{
    RaycastAllocatorRedirect(IUnityXRRaycastInterface* unityInterface, UnityXRRaycastDataAllocator* xrAllocator)
        : m_UnityInterface(unityInterface)
        , m_XrAllocator(xrAllocator)
    {}

    virtual UnityXRRaycastHit* UNITY_INTERFACE_API SetNumberOfHits(size_t numHits) override
    {
        return m_UnityInterface->Allocator_SetNumberOfHits(m_XrAllocator, numHits);
    }

    virtual UnityXRRaycastHit* UNITY_INTERFACE_API ExpandBy(size_t numHits)
    {
        return m_UnityInterface->Allocator_ExpandBy(m_XrAllocator, numHits);
    }

private:
    IUnityXRRaycastInterface* m_UnityInterface;
    UnityXRRaycastDataAllocator* m_XrAllocator;
};

UnitySubsystemErrorCode RaycastProvider::StaticRaycast(
    UnitySubsystemHandle handle, void* userData,
    float screenX, float screenY,
    UnityXRTrackableType xrHitFlags,
    UnityXRRaycastDataAllocator* xrAllocator)
{
    RaycastProvider* thiz = static_cast<RaycastProvider*>(userData);
    if (thiz == nullptr || xrAllocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    RaycastAllocatorRedirect redirect(thiz->m_UnityInterface, xrAllocator);
    return thiz->Raycast(screenX, screenY, xrHitFlags, redirect) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

void RaycastProvider::PopulateCStyleProvider(UnityXRRaycastProvider& xrProvider)
{
    std::memset(&xrProvider, 0, sizeof(xrProvider));
    xrProvider.userData = this;
    xrProvider.Raycast = &StaticRaycast;
}
