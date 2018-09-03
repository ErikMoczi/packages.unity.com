#include "MathConversion.h"
#include "mathfu/quaternion.h"
#include "Unity/IUnityXRPlane.deprecated.h"
#include "Utility.h"
#include "WrappedPlane.h"
#include "WrappedPose.h"

#include <cstring>

template<>
void WrappingBase<ArPlane>::ReleaseImpl()
{
    ArTrackable_release(ArAsTrackable(m_Ptr));
}

void WrappedPlane::ConvertToUnityXRPlane(UnityXRPlane& unityPlane, IUnityXRPlaneDataAllocator& allocator)
{
    ConvertToTrackableId(unityPlane.id, m_Ptr);

    WrappedPose centerPose = eWrappedConstruction::Default;
    ArPlane_getCenterPose(GetArSession(), m_Ptr, centerPose);

    centerPose.GetPosition(unityPlane.pose.position);
    centerPose.GetRotation(unityPlane.pose.rotation);
    std::memcpy(&unityPlane.center, &unityPlane.pose.position, sizeof(UnityXRVector3));

    ArPlane_getExtentX(GetArSession(), m_Ptr, &unityPlane.bounds.x);
    ArPlane_getExtentZ(GetArSession(), m_Ptr, &unityPlane.bounds.y);

    int32_t polygonSize = 0;
    ArPlane_getPolygonSize(GetArSession(), m_Ptr, &polygonSize);

    int32_t numVerts = polygonSize / 2;
    UnityXRVector3* boundaryVerts = allocator.AllocateBoundaryPoints(unityPlane.id, numVerts);

    float* boundaryVertsAsFloatPtr = reinterpret_cast<float*>(boundaryVerts);
    ArPlane_getPolygon(GetArSession(), m_Ptr, boundaryVertsAsFloatPtr);

    float* xzPair = boundaryVertsAsFloatPtr + 2 * (numVerts - 1);
    for (int32_t vertIndex = numVerts - 1; vertIndex >= 0; --vertIndex, xzPair -= 2)
    {
        UnityXRPose xrPose;
        ArPlane_getCenterPose(GetArSession(), m_Ptr, centerPose);
        centerPose.GetXrPose(xrPose);

        mathfu::Vector<float, 3> fuPosePosition;
        MathConversion::ToMathFu(fuPosePosition, xrPose.position);
        mathfu::Quaternion<float> fuPoseRotation;
        MathConversion::ToMathFu(fuPoseRotation, xrPose.rotation);

        float rawPlaneLocalPosition[3];
        rawPlaneLocalPosition[0] = xzPair[0];
        rawPlaneLocalPosition[1] = 0.0f;
        rawPlaneLocalPosition[2] = xzPair[1];

        mathfu::Vector<float, 3> fuPlaneLocalPosition;
        MathConversion::ToMathFu(fuPlaneLocalPosition, rawPlaneLocalPosition);

        mathfu::Vector<float, 3> fuTransformedPosition = fuPoseRotation * fuPlaneLocalPosition;
        fuTransformedPosition += fuPosePosition;

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

    WrappedPlane subsumedBy;
    unityPlane.wasMerged = AcquireSubsumedBy(subsumedBy);
    if (unityPlane.wasMerged)
        ConvertToTrackableId(unityPlane.mergedInto, subsumedBy.Get());

    // up to the calling function to fill this out as true when needed
    unityPlane.wasUpdated = false;
}

bool WrappedPlane::AcquireSubsumedBy(WrappedPlane& subsumedBy)
{
    ArPlane_acquireSubsumedBy(GetArSession(), m_Ptr, subsumedBy.ReleaseAndGetAddressOf());
    if (nullptr == subsumedBy)
        return false;

    subsumedBy.InitRefCount();
    return true;
}
