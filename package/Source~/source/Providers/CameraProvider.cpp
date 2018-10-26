#include "CameraImageApi.h"
#include "CameraProvider.h"
#include "MathConversion.h"
#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCameraIntrinsics.h"
#include "Wrappers/WrappedFrame.h"
#include "Wrappers/WrappedLightEstimate.h"
#include "Wrappers/WrappedPose.h"
#include "Wrappers/WrappedSession.h"

#include <cstring>
#include <GLES2/gl2.h>
#include <GLES2/gl2ext.h>

static bool g_HasColorCorrection = false;
static float g_LastColorCorrection[4] = {0, 0, 0, 0};

extern "C" bool UnityARCore_tryGetColorCorrection(float* red, float* green, float* blue, float* alpha)
{
    *red = g_LastColorCorrection[0];
    *green = g_LastColorCorrection[1];
    *blue = g_LastColorCorrection[2];
    *alpha = g_LastColorCorrection[3];
    return g_HasColorCorrection;
}

CameraProvider::CameraProvider()
    : m_LightEstimationEnabled(true)
    , m_HaveRetrievedMatrices(false)
    , m_ScreenWidth(-1.0f)
    , m_ScreenHeight(-1.0f)
{
    std::memset(&m_XrDisplayMatrix, 0, sizeof(m_XrDisplayMatrix));
    std::memset(&m_XrProjectionMatrix, 0, sizeof(m_XrProjectionMatrix));
}

const ArCamera* CameraProvider::GetArCamera() const
{
    return m_WrappedCamera;
}

ArCamera* CameraProvider::GetArCameraMutable()
{
    return m_WrappedCamera;
}

float CameraProvider::GetScreenWidth() const
{
    return m_ScreenWidth;
}

float CameraProvider::GetScreenHeight() const
{
    return m_ScreenHeight;
}

void CameraProvider::OnLifecycleInitialize()
{
    CameraImageApi::Create();
}

void CameraProvider::OnLifecycleShutdown()
{
    m_WrappedCamera.Release();
    CameraImageApi::Destroy();
}

bool UNITY_INTERFACE_API CameraProvider::GetFrame(const UnityXRCameraParams& xrParamsIn, UnityXRCameraFrame* frameOut)
{
    CameraImageApi::Update();

    WrappedSessionMutable wrappedSession = GetArSessionMutable();
    if (wrappedSession == nullptr)
        return false;

    if (GetArFrame() == nullptr)
        return false;

    m_ScreenWidth = xrParamsIn.screenWidth;
    m_ScreenHeight = xrParamsIn.screenHeight;

    frameOut->timestampNs = GetLatestTimestamp();
    frameOut->providedFields = kUnityXRCameraFramePropertiesTimestamp;

    if (!wrappedSession.TrySetDisplayGeometry(xrParamsIn.orientation, xrParamsIn.screenWidth, xrParamsIn.screenHeight))
        return false;

    RetrieveMatricesIfNeeded(xrParamsIn);
    if (m_HaveRetrievedMatrices)
    {
        frameOut->displayMatrix = m_XrDisplayMatrix;
        frameOut->projectionMatrix = m_XrProjectionMatrix;
        frameOut->providedFields = EnumCast<UnityXRCameraFramePropertyFlags>(
            frameOut->providedFields |
            kUnityXRCameraFramePropertiesProjectionMatrix |
            kUnityXRCameraFramePropertiesDisplayMatrix);
    }

    {
        WrappedLightEstimateRaii wrappedLightEstimate;
        wrappedLightEstimate.GetFromFrame();
        wrappedLightEstimate.GetColorCorrection(g_LastColorCorrection);
        ArLightEstimateState lightEstimateState = wrappedLightEstimate.GetState();

        if (lightEstimateState == AR_LIGHT_ESTIMATE_STATE_VALID)
        {
            g_HasColorCorrection = true;
            frameOut->averageBrightness = g_LastColorCorrection[3];
            frameOut->providedFields = EnumCast<UnityXRCameraFramePropertyFlags>(frameOut->providedFields | kUnityXRCameraFramePropertiesAverageBrightness);
        }
        else
        {
            g_HasColorCorrection = false;
        }
    }

    frameOut->numTextures = 1;
    UnityXRTextureDescriptor& textureDescOut = frameOut->textureDescriptors[0];

    uint32_t cameraTextureName = GetCameraTextureName();
    textureDescOut.nativeId = cameraTextureName;
    std::strncpy(textureDescOut.name, "_MainTex", kUnityXRStringSize);

    // TODO: fix this hard-coding
    textureDescOut.format = kUnityRenderingExtFormatR8G8B8A8_SRGB;

    WrappedCameraIntrinsicsRaii wrappedIntrinsics;
    wrappedIntrinsics.GetFromCameraTexture();
    wrappedIntrinsics.GetImageDimensions(textureDescOut.width, textureDescOut.height);
    return true;
}

void UNITY_INTERFACE_API CameraProvider::SetLightEstimationRequested(bool requested)
{
    if (requested)
        GetSessionProviderMutable().RequestStartLightEstimation();
    else
        GetSessionProviderMutable().RequestStopLightEstimation();
}

bool UNITY_INTERFACE_API CameraProvider::GetShaderName(char(&shaderName)[kUnityXRStringSize])
{
    strncpy(shaderName, "Unlit/ARCoreBackground", kUnityXRStringSize);
    return true;
}

void CameraProvider::PopulateCStyleProvider(UnityXRCameraProvider& xrProvider)
{
    std::memset(&xrProvider, 0, sizeof(xrProvider));
    xrProvider.userData = this;
    xrProvider.GetFrame = &StaticGetFrame;
    xrProvider.SetLightEstimationRequested = &StaticSetLightEstimationRequested;
    xrProvider.GetShaderName = &StaticGetShaderName;
}

void CameraProvider::AcquireCameraFromNewFrame()
{
    m_WrappedCamera.AcquireFromFrame();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API CameraProvider::StaticGetFrame(UnitySubsystemHandle handle, void* userData, const UnityXRCameraParams* xrParamsIn, UnityXRCameraFrame* xrFrameOut)
{
    CameraProvider* thiz = static_cast<CameraProvider*>(userData);
    if (thiz == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    return thiz->GetFrame(*xrParamsIn, xrFrameOut) ? kUnitySubsystemErrorCodeSuccess : kUnitySubsystemErrorCodeFailure;
}

void UNITY_INTERFACE_API CameraProvider::StaticSetLightEstimationRequested(UnitySubsystemHandle handle, void* userData, bool requested)
{
    CameraProvider* thiz = static_cast<CameraProvider*>(userData);
    if (thiz == nullptr)
        return;

    thiz->SetLightEstimationRequested(requested);
}

UnitySubsystemErrorCode UNITY_INTERFACE_API CameraProvider::StaticGetShaderName(UnitySubsystemHandle handle, void* userData, char shaderName[kUnityXRStringSize])
{
    if (shaderName == nullptr)
        return kUnitySubsystemErrorCodeInvalidArguments;

    strncpy(shaderName, "Unlit/ARCoreBackground", kUnityXRStringSize);
    return kUnitySubsystemErrorCodeSuccess;
}

void CameraProvider::RetrieveMatricesIfNeeded(const UnityXRCameraParams& xrParamsIn)
{
    // We can early out if the matrices haven't changed.
    // * If the zNear or zFar has changed, then we need to recompute them
    // * If ArFrame_getDisplayGeometryChanged has /never/ returned true, then
    //   we need to wait for that before computing the matrices.

    WrappedFrame wrappedFrame = GetArFrame();
    if (wrappedFrame == nullptr)
        return;

    const bool didGeometryChange = wrappedFrame.DidDisplayGeometryChange();

    if (!m_HaveRetrievedMatrices && !didGeometryChange)
        return;
    else if (didGeometryChange)
        m_HaveRetrievedMatrices = true;

    enum
    {
        kBasisU1,
        kBasisV1,
        kBasisU2,
        kBasisV2,
        kOffsetU,
        kOffsetV,
        kNumUVTransformElements
    };

    if (didGeometryChange)
    {
        float uvsToTransform[kNumUVTransformElements];
        uvsToTransform[kBasisU1] = 1.0f;
        uvsToTransform[kBasisV1] = 0.0f;
        uvsToTransform[kBasisU2] = 0.0f;
        uvsToTransform[kBasisV2] = 1.0f;
        uvsToTransform[kOffsetU] = 0.0f;
        uvsToTransform[kOffsetV] = 0.0f;

        float transformedUVs[kNumUVTransformElements];
        wrappedFrame.TransformDisplayUvCoords(kNumUVTransformElements, uvsToTransform, transformedUVs);

        m_XrDisplayMatrix.columns[0].x = transformedUVs[kBasisU1] - transformedUVs[kOffsetU];
        m_XrDisplayMatrix.columns[0].y = transformedUVs[kBasisV1] - transformedUVs[kOffsetV];
        m_XrDisplayMatrix.columns[1].x = transformedUVs[kBasisU2] - transformedUVs[kOffsetU];
        m_XrDisplayMatrix.columns[1].y = transformedUVs[kBasisV2] - transformedUVs[kOffsetV];
        m_XrDisplayMatrix.columns[2].x = transformedUVs[kOffsetU];
        m_XrDisplayMatrix.columns[2].y = transformedUVs[kOffsetV];
    }

    if (m_CachedZFar != xrParamsIn.zFar || m_CachedZNear != xrParamsIn.zNear || didGeometryChange)
    {
        m_CachedZNear = xrParamsIn.zNear;
        m_CachedZFar = xrParamsIn.zFar;
        m_WrappedCamera.GetProjectionMatrix(m_XrProjectionMatrix, xrParamsIn.zNear, xrParamsIn.zFar);
    }
}
