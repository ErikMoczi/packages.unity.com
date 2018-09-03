#include "RaycastProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedHitResult.h"
#include "Wrappers/WrappedHitResultList.h"
#include "Wrappers/WrappedPose.h"
#include "Wrappers/WrappedTrackable.h"

#include <cstring>
#include <vector>

// TODO: need to revisit this - ARCore doesn't seem to have the concept of hit-testing against infinite planes that ARKit must have
static bool DoTrackableTypesMatch(ArTrackableType googleType, UnityXRTrackableType unityType)
{
    switch (googleType)
    {
    case AR_TRACKABLE_PLANE:
        return (unityType & kUnityXRTrackableTypePlaneWithinPolygon) != 0;

    case AR_TRACKABLE_POINT:
        return (unityType & kUnityXRTrackableTypePoint) != 0;
    }

    return false;
}

static UnityXRTrackableType GetHitType(
    const WrappedTrackable& trackable,
    const WrappedPose& pose,
    UnityXRTrackableType hitFlags)
{
    UnityXRTrackableType hitType = kUnityXRTrackableTypeNone;
    const ArTrackableType trackableType = trackable.GetType();

    switch (trackableType)
    {
        case AR_TRACKABLE_PLANE:
        {
            if (hitFlags & kUnityXRTrackableTypePlaneWithinInfinity)
                hitType = static_cast<UnityXRTrackableType>(hitType | kUnityXRTrackableTypePlaneWithinInfinity);

            ///
            // Check polygon and bounds
            //
            const ArPlane* plane = reinterpret_cast<const ArPlane*>(trackable.Get());

            if (hitFlags & kUnityXRTrackableTypePlaneWithinPolygon)
            {
                int32_t isInside = 0;
                ArPlane_isPoseInPolygon(GetArSession(), plane, pose, &isInside);
                if (isInside != 0)
                    hitType = static_cast<UnityXRTrackableType>(hitType | kUnityXRTrackableTypePlaneWithinPolygon);
            }

            if (hitFlags & kUnityXRTrackableTypePlaneWithinBounds)
            {
                int32_t isInside = 0;
                ArPlane_isPoseInExtents(GetArSession(), plane, pose, &isInside);
                if (isInside != 0)
                    hitType = static_cast<UnityXRTrackableType>(hitType | kUnityXRTrackableTypePlaneWithinBounds);
            }

            break;
        }

        case AR_TRACKABLE_POINT:
        {
            if (hitFlags & kUnityXRTrackableTypePoint)
                hitType = static_cast<UnityXRTrackableType>(hitType | kUnityXRTrackableTypePoint);
            break;
        }
    }

    return hitType;
}

// TODO: return false under the right circumstances (tracking loss, at least - maybe more?)
bool UNITY_INTERFACE_API RaycastProvider::Raycast(
    float screenX, float screenY,
    UnityXRTrackableType hitFlags,
    IUnityXRRaycastAllocator& allocator)
{
    const float screenWidth = GetScreenWidth();
    const float screenHeight = GetScreenHeight();
    if (screenWidth < 0.0f || screenHeight < 0.0f)
        return false;

    screenY = 1.0f - screenY;
    screenX *= screenWidth;
    screenY *= screenHeight;

    WrappedHitResultList hitResultsGoogle = eWrappedConstruction::Default;
    hitResultsGoogle.HitTest(screenX, screenY);

    std::vector<UnityXRRaycastHit> hitResultsUnity;
    hitResultsUnity.reserve(hitResultsGoogle.Size());

    for (int32_t hitResultIndex = 0; hitResultIndex < hitResultsGoogle.Size(); ++hitResultIndex)
    {
        UnityXRRaycastHit unityHitResult;

        // Get the hit result from ARCore
        WrappedHitResult googleHitResult = eWrappedConstruction::Default;
        hitResultsGoogle.GetHitResultAt(hitResultIndex, googleHitResult);

        // Extract the trackable
        WrappedTrackable trackable;
        googleHitResult.AcquireTrackable(trackable);

        // Extract the pose
        WrappedPose pose = eWrappedConstruction::Default;
        ArHitResult_getHitPose(GetArSession(), googleHitResult, pose);
        pose.GetXrPose(unityHitResult.pose);

        // Fill out the Unity struct
        unityHitResult.hitType = GetHitType(trackable, pose, hitFlags);

        // Skip if it doesn't match the filter
        if (unityHitResult.hitType == kUnityXRTrackableTypeNone)
            continue;

        ConvertToTrackableId(unityHitResult.trackableId, trackable.Get());
        unityHitResult.distance = googleHitResult.GetDistance();
        hitResultsUnity.push_back(unityHitResult);
    }

    if (hitResultsUnity.empty())
        return false;

    UnityXRRaycastHit* unityAllocatedResults = allocator.SetNumberOfHits(hitResultsUnity.size());
    std::memcpy(unityAllocatedResults, hitResultsUnity.data(), sizeof(UnityXRRaycastHit) * hitResultsUnity.size());
    return true;
}
