#include "DepthProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedPointCloud.h"

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API DepthProvider::GetPointCloud(IUnityXRDepthDataAllocator& allocator)
{
    WrappedPointCloud pointCloud = eWrappedConstruction::Default;
    int32_t numPoints = pointCloud.GetNumPoints();
    allocator.SetNumberOfPoints(numPoints);

    // TODO: might want to profile this against an implementation where we split
    //       up the point- and confidence-populating into separate loops to test
    //       for whether potential lack of cache coherency is an issue on-device
    UnityXRVector3* pointsBuffer = allocator.GetPointsBuffer();
    float* confidenceBuffer = allocator.GetConfidenceBuffer();
    for (int32_t pointIndex = 0; pointIndex < numPoints; ++pointIndex)
    {
        pointCloud.GetPositionAt(pointIndex, pointsBuffer[pointIndex]);
        confidenceBuffer[pointIndex] = pointCloud.GetConfidenceAt(pointIndex);
    }

    return true;
}
