#include "DepthProvider.h"
#include "Utility.h"

#include "Wrappers/WrappedPointCloud.h"

static const int kNumFloatsPerPoint = 4;

DepthProvider::DepthProvider(IUnityXRDepthInterface*& unityInterface)
    : m_UnityInterface(unityInterface)
{
}

struct DepthDataAllocatorRedirect : public IUnityXRDepthDataAllocator
{
    DepthDataAllocatorRedirect(const UnityXRDepthDataAllocator* xrAllocator, IUnityXRDepthInterface* unityInterface)
        : m_XrAllocator(xrAllocator)
        , m_UnityInterface(unityInterface)
    {}

    virtual void UNITY_INTERFACE_API SetNumberOfPoints(size_t numPoints) override
    {
        m_UnityInterface->Allocator_SetNumberOfPoints(m_XrAllocator, numPoints);
    }

    virtual UnityXRVector3* UNITY_INTERFACE_API GetPointsBuffer() const override
    {
        return m_UnityInterface->Allocator_GetPointsBuffer(m_XrAllocator);
    }

    virtual float* UNITY_INTERFACE_API GetConfidenceBuffer() const override
    {
        return m_UnityInterface->Allocator_GetConfidenceBuffer(m_XrAllocator);
    }

private:
	const UnityXRDepthDataAllocator* m_XrAllocator;
	IUnityXRDepthInterface* m_UnityInterface;
};

// TODO: return false under the right circumstances (I'm guessing at least during tracking loss... anything else?)
bool UNITY_INTERFACE_API DepthProvider::GetPointCloud(IUnityXRDepthDataAllocator& xrAllocator)
{
    if (GetArSession() == nullptr)
        return false;

    if (GetArFrame() == nullptr)
        return false;

    WrappedPointCloudRaii wrappedPointCloud;
    auto status = wrappedPointCloud.TryAcquireFromFrame();
    if (ARSTATUS_FAILED(status))
        return false;

    const float* data = nullptr;
    wrappedPointCloud.GetData(data);

    int32_t numPoints = wrappedPointCloud.NumPoints();
    xrAllocator.SetNumberOfPoints(numPoints);

    // TODO: might want to profile this against an implementation where we split
    //       up the point- and confidence-populating into separate loops to test
    //       for whether potential lack of cache coherency is an issue on-device
    UnityXRVector3* xrPointsBuffer = xrAllocator.GetPointsBuffer();
    float* xrConfidenceBuffer = xrAllocator.GetConfidenceBuffer();
    for (int32_t pointIndex = 0; pointIndex < numPoints; ++pointIndex, data += kNumFloatsPerPoint, ++xrPointsBuffer, ++xrConfidenceBuffer)
    {
        xrPointsBuffer->x = data[0];
        xrPointsBuffer->y = data[1];
        xrPointsBuffer->z = -data[2];
        *xrConfidenceBuffer = data[3];
    }

    return true;
}

void DepthProvider::PopulateCStyleProvider(UnityXRDepthProvider& xrProvider)
{
	std::memset(&xrProvider, 0, sizeof(xrProvider));
	xrProvider.userData = this;
	xrProvider.GetPointCloud = &StaticGetPointCloud;
}

UnitySubsystemErrorCode UNITY_INTERFACE_API DepthProvider::StaticGetPointCloud(UnitySubsystemHandle handle, void* userData, const UnityXRDepthDataAllocator* xrAllocator)
{
    DepthProvider* thiz = static_cast<DepthProvider*>(userData);
    if (thiz == nullptr || xrAllocator == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    DepthDataAllocatorRedirect redirect(xrAllocator, thiz->m_UnityInterface);
    return thiz->GetPointCloud(redirect) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}
