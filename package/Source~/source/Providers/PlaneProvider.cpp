#include "PlaneProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPlaneList.h"
#include "Wrappers/WrappedSession.h"

#include <cstring>
#include <set>

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API PlaneProvider::GetAllPlanes(IUnityXRPlaneDataAllocator& allocator)
{
    const int64_t latestFrameTimestamp = GetWrappedSession().GetTimestamp();
    if (latestFrameTimestamp == m_LastFrameTimestamp)
        return false;

    std::set<UnityXRTrackableId> updatedIds;
    {
        WrappedPlaneList updatedPlanes = eWrappedConstruction::Default;
        updatedPlanes.GetUpdatedPlanes();

        const int32_t numUpdatedPlanes = updatedPlanes.Size();
        for (int32_t planeIndex = 0; planeIndex < numUpdatedPlanes; ++planeIndex)
        {
            WrappedPlane updatedPlane;
            if (!updatedPlanes.TryAcquireAt(planeIndex, updatedPlane))
                continue;

            UnityXRTrackableId id;
            ConvertToTrackableId(id, updatedPlane.Get());
            updatedIds.insert(id);
        }
    }

    WrappedPlaneList detectedPlanes = eWrappedConstruction::Default;
    detectedPlanes.GetAllPlanes();

    const int32_t numPlanes = detectedPlanes.Size();
    UnityXRPlane* currentUnityPlane = allocator.AllocatePlaneData(numPlanes);
    for (int32_t planeIndex = 0; planeIndex < numPlanes; ++planeIndex, ++currentUnityPlane)
    {
        WrappedPlane wrappedPlane;
        if (!detectedPlanes.TryAcquireAt(planeIndex, wrappedPlane))
            continue;

        wrappedPlane.ConvertToUnityXRPlane(*currentUnityPlane, allocator);
        currentUnityPlane->wasUpdated = updatedIds.find(currentUnityPlane->id) != updatedIds.end();
    }

    m_LastFrameTimestamp = latestFrameTimestamp;

    return true;
}
