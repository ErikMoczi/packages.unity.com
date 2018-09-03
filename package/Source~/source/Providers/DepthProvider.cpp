#include "DepthProvider.h"
#include "Utility.h"

static const int kNumFloatsPerPoint = 4;
static const int kOffsetToConfindenceWithinPoint = 3;

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API DepthProvider::GetPointCloud(IUnityXRDepthDataAllocator& allocator)
{
    auto session = GetArSession();
    if (session == nullptr)
        return false;

    auto frame = GetArFrame();
    if (frame == nullptr)
        return false;

    ArPointCloud* pointCloud;
    ArStatus ars = ArFrame_acquirePointCloud(session, frame, &pointCloud);
    if (ARSTATUS_FAILED(ars))
        return false;

    const float* data;
    ArPointCloud_getData(session, pointCloud, &data);

    int32_t numPoints;
    ArPointCloud_getNumberOfPoints(session, pointCloud, &numPoints);
    allocator.SetNumberOfPoints(numPoints);

    // TODO: might want to profile this against an implementation where we split
    //       up the point- and confidence-populating into separate loops to test
    //       for whether potential lack of cache coherency is an issue on-device
    UnityXRVector3* pointsBuffer = allocator.GetPointsBuffer();
    float* confidenceBuffer = allocator.GetConfidenceBuffer();
    for (int32_t pointIndex = 0; pointIndex < numPoints; ++pointIndex, data += kNumFloatsPerPoint, ++pointsBuffer, ++confidenceBuffer)
    {
        pointsBuffer->x = data[0];
        pointsBuffer->y = data[1];
        pointsBuffer->z = -data[2];
        *confidenceBuffer = data[3];
    }

    ArPointCloud_release(pointCloud);

    return true;
}
