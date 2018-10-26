#pragma once
#include <stdint.h>

/// A unique id assignable to a 'trackable', e.g. a plane or
/// point cloud point. Although this id is 128 bits, it may
/// not necessarily be globally unique; it just needs to be
/// unique to a particular XREnvironment session.
typedef struct UnityXRTrackableId
{
    uint64_t idPart[2];
} UnityXRTrackableId;

#ifdef __cplusplus
inline bool operator==(const UnityXRTrackableId& a, const UnityXRTrackableId& b)
{
    return
        (a.idPart[0] == b.idPart[0]) &&
        (a.idPart[1] == b.idPart[1]);
}

inline bool operator!=(const UnityXRTrackableId& a, const UnityXRTrackableId& b)
{
    return
        (a.idPart[0] != b.idPart[0]) ||
        (a.idPart[1] != b.idPart[1]);
}

/// A comparator for UnityXRTrackableId suitable for algorithms or containers that
/// require ordered UnityXRTrackableIds, e.g. std::map
struct UnityXRTrackableIdLessThanComparator
{
    bool operator()(const UnityXRTrackableId& lhs, const UnityXRTrackableId& rhs)
    {
        if (lhs.idPart[0] == rhs.idPart[0])
        {
            return lhs.idPart[1] < rhs.idPart[1];
        }

        return lhs.idPart[0] < rhs.idPart[0];
    }
};
#endif // __cplusplus

/// Flags representing trackable types, e.g. planes or feature points,
/// as well as more specific attributes of those trackables.
typedef enum UnityXRTrackableType
{
    /// No trackable
    kUnityXRTrackableTypeNone = 0,

    /// A position within the plane's boundary
    kUnityXRTrackableTypePlaneWithinPolygon = 1 << 0,

    /// A position within the plane's bounds (the 4 corners)
    kUnityXRTrackableTypePlaneWithinBounds = 1 << 1,

    /// A point on a plane with infinite bounds
    kUnityXRTrackableTypePlaneWithinInfinity = 1 << 2,

    /// A point on an estimated plane
    kUnityXRTrackableTypePlaneEstimated = 1 << 3,

    /// Flag combining all types of planes
    kUnityXRTrackableTypePlanes =
        kUnityXRTrackableTypePlaneWithinPolygon |
        kUnityXRTrackableTypePlaneWithinBounds |
        kUnityXRTrackableTypePlaneWithinInfinity |
        kUnityXRTrackableTypePlaneEstimated,

    /// A point cloud point
    kUnityXRTrackableTypePoint = 1 << 4,

    /// Flag combing all types of trackables
    kUnityXRTrackableTypeAll =
        kUnityXRTrackableTypePlanes |
        kUnityXRTrackableTypePoint
} UnityXRTrackableType;

/// The tracking state of the device.
typedef enum UnityXRTrackingState
{
    /// Unknown tracking state.
    kUnityXRTrackingStateUnknown,

    /// Tracking data is available.
    kUnityXRTrackingStateTracking,

    /// Tracking data is unavailable, e.g., because the
    /// session is not running, or the camera is being covered.
    kUnityXRTrackingStateUnavailable
} UnityXRTrackingState;
