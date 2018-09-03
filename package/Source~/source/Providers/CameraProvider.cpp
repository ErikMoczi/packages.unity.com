#include "CameraProvider.h"
#include "MathConversion.h"
#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedLightEstimate.h"
#include "Wrappers/WrappedPose.h"

#include <cstring>
#include <GLES2/gl2.h>
#include <GLES2/gl2ext.h>

CameraProvider::CameraProvider()
    : m_LightEstimationEnabled(true)
    , m_HaveRetrievedMatrices(false)
    , m_ScreenWidth(-1.0f)
    , m_ScreenHeight(-1.0f)
{
    std::memset(&m_DisplayMatrix, 0, sizeof(m_DisplayMatrix));
    std::memset(&m_ProjectionMatrix, 0, sizeof(m_ProjectionMatrix));
}

const WrappedCamera& CameraProvider::GetWrappedCamera() const
{
    return m_WrappedCamera;
}

WrappedCamera& CameraProvider::GetWrappedCameraMutable()
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

void CameraProvider::OnLifecycleShutdown()
{
    m_WrappedCamera.Release();
}

// what should we do when the light estimation mode is AR_LIGHT_ESTIMATION_MODE_DISABLED?
// and when the light estimate state is AR_LIGHT_ESTIMATE_STATE_INVALID?
static bool TryGetLightEstimatePixelIntensity(float& outPixelIntensity)
{
    WrappedLightEstimate lightEstimate = eWrappedConstruction::Default;
    lightEstimate.GetFromFrame();

    if (lightEstimate.GetState() != AR_LIGHT_ESTIMATE_STATE_VALID)
        return false;

    outPixelIntensity = lightEstimate.GetPixelIntensity();
    return true;
}

const int kInvalidOrientation = -1;
static int ConvertUnityToGoogleOrientation(UnityXRScreenOrientation unityOrientation)
{
    switch (unityOrientation)
    {
    case kUnityXRScreenOrientationPortrait:
        return 0; // ROTATION_0

    case kUnityXRScreenOrientationPortraitUpsideDown:
        return 2; // ROTATION_180

    case kUnityXRScreenOrientationLandscapeLeft:
        return 1; // ROTATION_90

    case kUnityXRScreenOrientationLandscapeRight:
        return 3; // ROTATION_270
    }

    return kInvalidOrientation;
}

bool UNITY_INTERFACE_API CameraProvider::GetFrame(const UnityXRCameraParams& paramsIn, UnityXRCameraFrame* frameOut)
{
    auto session = GetArSession();
    if (session == nullptr)
        return false;

    auto frame = GetArFrame();
    if (frame == nullptr)
        return false;

    m_ScreenWidth = paramsIn.screenWidth;
    m_ScreenHeight = paramsIn.screenHeight;

    ArFrame_getTimestamp(session, frame, &frameOut->timestampNs);
    frameOut->providedFields = kUnityXRCameraFramePropertiesTimestamp;

    int googleOrientation = ConvertUnityToGoogleOrientation(paramsIn.orientation);
    if (kInvalidOrientation == googleOrientation)
        return false;

    ArSession_setDisplayGeometry(session, googleOrientation, paramsIn.screenWidth, paramsIn.screenHeight);

    if (frame != nullptr)
        RetrieveMatricesIfNeeded(session, frame, paramsIn);

    if (m_HaveRetrievedMatrices)
    {
        frameOut->displayMatrix = m_DisplayMatrix;
        frameOut->projectionMatrix = m_ProjectionMatrix;
        frameOut->providedFields = EnumCast<UnityXRCameraFramePropertyFlags>(
            frameOut->providedFields |
            kUnityXRCameraFramePropertiesProjectionMatrix |
            kUnityXRCameraFramePropertiesDisplayMatrix);
    }

    if (TryGetLightEstimatePixelIntensity(frameOut->averageBrightness))
        frameOut->providedFields = EnumCast<UnityXRCameraFramePropertyFlags>(frameOut->providedFields | kUnityXRCameraFramePropertiesAverageBrightness);

    frameOut->numTextures = 1;
    UnityXRTextureDescriptor& textureDescOut = frameOut->textureDescriptors[0];

    uint32_t cameraTextureName = GetCameraTextureName();
    textureDescOut.nativeId = cameraTextureName;

    // TODO: fix this hard-coding
    textureDescOut.format = kUnityRenderingExtFormatA8R8G8B8_SRGB;

    // TODO: plumb through proper values
    textureDescOut.width = 2880;
    textureDescOut.height = 1440;

    return true;
}

void UNITY_INTERFACE_API CameraProvider::SetLightEstimationRequested(bool requested)
{
    if (requested)
        GetSessionProviderMutable().RequestStartLightEstimation();
    else
        GetSessionProviderMutable().RequestStartLightEstimation();
}

bool UNITY_INTERFACE_API CameraProvider::GetShaderName(char(&shaderName)[kUnityXRStringSize])
{
    strncpy(shaderName, "Unlit/ARCoreBackground", kUnityXRStringSize);
    return true;
}

void CameraProvider::RetrieveMatricesIfNeeded(ArSession* session, ArFrame* frame, const UnityXRCameraParams& paramsIn)
{
    // We can early out if the matrices haven't changed.
    // * If the zNear or zFar has changed, then we need to recompute them
    // * If ArFrame_getDisplayGeometryChanged has /never/ returned true, then
    //   we need to wait for that before computing the matrices.

    int32_t didGeometryChange;
    ArFrame_getDisplayGeometryChanged(session, frame, &didGeometryChange);

    if (!m_HaveRetrievedMatrices && didGeometryChange == 0)
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
        ArFrame_transformDisplayUvCoords(session, frame, kNumUVTransformElements, uvsToTransform, transformedUVs);

        m_DisplayMatrix.columns[0].x = transformedUVs[kBasisU1] - transformedUVs[kOffsetU];
        m_DisplayMatrix.columns[0].y = transformedUVs[kBasisV1] - transformedUVs[kOffsetV];
        m_DisplayMatrix.columns[1].x = transformedUVs[kBasisU2] - transformedUVs[kOffsetU];
        m_DisplayMatrix.columns[1].y = transformedUVs[kBasisV2] - transformedUVs[kOffsetV];
        m_DisplayMatrix.columns[2].x = transformedUVs[kOffsetU];
        m_DisplayMatrix.columns[2].y = transformedUVs[kOffsetV];
    }

    if (m_CachedZFar != paramsIn.zFar || m_CachedZNear != paramsIn.zNear || didGeometryChange)
    {
        m_CachedZNear = paramsIn.zNear;
        m_CachedZFar = paramsIn.zFar;
        m_WrappedCamera.GetProjectionMatrix(m_ProjectionMatrix, paramsIn.zNear, paramsIn.zFar);
    }
}
