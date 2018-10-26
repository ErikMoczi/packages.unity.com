#include "MathConversion.h"
#include "Utility.h"
#include "WrappedPlane.h"
#include "WrappedPose.h"

#include "mathfu/quaternion.h"
#include "Unity/IUnityXRPlane.deprecated.h"

#include <cstring>

WrappedPlane::WrappedPlane()
    : m_ArPlane(nullptr)
{
}

WrappedPlane::WrappedPlane(const ArPlane* arPlane)
    : m_ArPlane(arPlane)
{
}

WrappedPlane::operator const ArPlane*() const
{
    return m_ArPlane;
}

const ArPlane* WrappedPlane::Get() const
{
    return m_ArPlane;
}

void WrappedPlane::ConvertToXRPlane(UnityXRPlane& xrPlane, IUnityXRPlaneDataAllocator& xrAllocator) const
{
    ConvertToTrackableId(xrPlane.id, m_ArPlane);

    GetCenterPose(xrPlane.pose);
    std::memcpy(&xrPlane.center, &xrPlane.pose.position, sizeof(UnityXRVector3));

    GetExtents(xrPlane.bounds.x, xrPlane.bounds.y);

    mathfu::Vector<float, 3> fuCenterPosition;
    mathfu::Quaternion<float> fuCenterRotation;
    MathConversion::ToMathFu(fuCenterPosition, xrPlane.pose.position);
    MathConversion::ToMathFu(fuCenterRotation, xrPlane.pose.rotation);

    const int32_t polygonSize = GetPolygonSize();

    int32_t numVerts = polygonSize / 2;
    UnityXRVector3* boundaryVerts = xrAllocator.AllocateBoundaryPoints(xrPlane.id, numVerts);

    float* boundaryVertsAsFloatPtr = reinterpret_cast<float*>(boundaryVerts);
    GetPolygon(boundaryVertsAsFloatPtr);

    float* xzPair = boundaryVertsAsFloatPtr + 2 * (numVerts - 1);
    for (int32_t vertIndex = numVerts - 1; vertIndex >= 0; --vertIndex, xzPair -= 2)
    {
        float rawPlaneLocalPosition[3];
        rawPlaneLocalPosition[0] = xzPair[0];
        rawPlaneLocalPosition[1] = 0.0f;
        rawPlaneLocalPosition[2] = xzPair[1];

        mathfu::Vector<float, 3> fuPlaneLocalPosition;
        MathConversion::ToMathFu(fuPlaneLocalPosition, rawPlaneLocalPosition);

        mathfu::Vector<float, 3> fuTransformedPosition = fuCenterRotation * fuPlaneLocalPosition;
        fuTransformedPosition += fuCenterPosition;

        MathConversion::ToUnity(boundaryVerts[vertIndex], fuTransformedPosition);
    }

    // Reverse it -- need clockwise winding order
    for (int32_t vertIndex = 0; vertIndex < numVerts / 2; ++vertIndex)
    {
        const int32_t otherVertIndex = numVerts - 1 - vertIndex;
        const UnityXRVector3 temp = boundaryVerts[vertIndex];

        boundaryVerts[vertIndex] = boundaryVerts[otherVertIndex];
        boundaryVerts[otherVertIndex] = temp;
    }

    WrappedPlaneRaii wrappedSubsumingPlane;
    xrPlane.wasMerged = wrappedSubsumingPlane.TryAcquireSubsumedBy(m_ArPlane);
    if (xrPlane.wasMerged)
        ConvertToTrackableId(xrPlane.mergedInto, wrappedSubsumingPlane.Get());

    // up to the calling function to fill this out as true when needed
    xrPlane.wasUpdated = false;
}

bool WrappedPlane::IsPoseInPolygon(const ArPose* arPose) const
{
    int32_t retAsInt = 0;
    ArPlane_isPoseInPolygon(GetArSession(), m_ArPlane, arPose, &retAsInt);
    return retAsInt != 0;
}

bool WrappedPlane::IsPoseInPolygon(const UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose = xrPose;
    return IsPoseInPolygon(wrappedPose);
}

bool WrappedPlane::IsPoseInExtents(const ArPose* arPose) const
{
    int32_t retAsInt = 0;
    ArPlane_isPoseInExtents(GetArSession(), m_ArPlane, arPose, &retAsInt);
    return retAsInt != 0;
}

bool WrappedPlane::IsPoseInExtents(const UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose = xrPose;
    return IsPoseInPolygon(wrappedPose);
}

void WrappedPlane::GetExtents(float& x, float& z) const
{
    ArPlane_getExtentX(GetArSession(), m_ArPlane, &x);
    ArPlane_getExtentZ(GetArSession(), m_ArPlane, &z);
}

int32_t WrappedPlane::GetPolygonSize() const
{
    int32_t ret = 0;
    ArPlane_getPolygonSize(GetArSession(), m_ArPlane, &ret);
    return ret;
}

void WrappedPlane::GetPolygon(float* boundaryVerts) const
{
    ArPlane_getPolygon(GetArSession(), m_ArPlane, boundaryVerts);
}

void WrappedPlane::GetCenterPose(ArPose* arPose) const
{
    ArPlane_getCenterPose(GetArSession(), m_ArPlane, arPose);
}

void WrappedPlane::GetCenterPose(UnityXRPose& xrPose) const
{
    WrappedPoseRaii wrappedPose = GetIdentityPose();
    GetCenterPose(wrappedPose);
    wrappedPose.GetPose(xrPose);
}

WrappedPlaneMutable::WrappedPlaneMutable()
{
}

WrappedPlaneMutable::WrappedPlaneMutable(ArPlane* arPlane)
    : WrappedPlane(arPlane)
{
}

WrappedPlaneMutable::operator ArPlane*()
{
    return GetArPlaneMutable();
}

ArPlane* WrappedPlaneMutable::Get()
{
    return GetArPlaneMutable();
}

ArPlane*& WrappedPlaneMutable::GetArPlaneMutable()
{
    return *const_cast<ArPlane**>(&m_ArPlane);
}

WrappedPlaneRaii::WrappedPlaneRaii()
{
}

WrappedPlaneRaii::~WrappedPlaneRaii()
{
    Release();
}

bool WrappedPlaneRaii::TryAcquireSubsumedBy(const ArPlane* planeSubsumed)
{
    Release();
    ArPlane_acquireSubsumedBy(GetArSession(), planeSubsumed, &GetArPlaneMutable());
    return m_ArPlane != nullptr;
}

void WrappedPlaneRaii::Release()
{
    if (m_ArPlane == nullptr)
        return;

    ArTrackable_release(ArAsTrackable(GetArPlaneMutable()));
    m_ArPlane = nullptr;
}
