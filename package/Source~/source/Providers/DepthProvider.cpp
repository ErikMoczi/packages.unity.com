#include "DepthProvider.h"
#include "Utility.h"

static const int kNumFloatsPerPoint = 4;
static const int kOffsetToConfindenceWithinPoint = 3;

DepthProvider::DepthProvider(IUnityXRDepthInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
{
}

struct DepthDataAllocatorWrapper : public IUnityXRDepthDataAllocator
{
    DepthDataAllocatorWrapper(const UnityXRDepthDataAllocator* allocator, IUnityXRDepthInterface* unityInterface)
        : m_Allocator(allocator)
        , m_UnityInterface(unityInterface)
    {}

    virtual void UNITY_INTERFACE_API SetNumberOfPoints(size_t numPoints) override
    {
        m_UnityInterface->Allocator_SetNumberOfPoints(m_Allocator, numPoints);
    }

    virtual UnityXRVector3* UNITY_INTERFACE_API GetPointsBuffer() const override
    {
        return m_UnityInterface->Allocator_GetPointsBuffer(m_Allocator);
    }

    virtual float* UNITY_INTERFACE_API GetConfidenceBuffer() const override
    {
        return m_UnityInterface->Allocator_GetConfidenceBuffer(m_Allocator);
    }

private:
	const UnityXRDepthDataAllocator* m_Allocator;
	IUnityXRDepthInterface* m_UnityInterface;
};

UnitySubsystemErrorCode UNITY_INTERFACE_API DepthProvider::StaticGetPointCloud(UnitySubsystemHandle handle, void* userData, const UnityXRDepthDataAllocator* allocator)
{
    DepthProvider* provider = static_cast<DepthProvider*>(userData);
    if (provider == nullptr || allocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    DepthDataAllocatorWrapper wrapper(allocator, provider->m_UnityInterface);
    return provider->GetPointCloud(wrapper) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

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

void DepthProvider::PopulateCStyleProvider(UnityXRDepthProvider& provider)
{
	std::memset(&provider, 0, sizeof(provider));
	provider.userData = this;
	provider.GetPointCloud = &StaticGetPointCloud;
}
