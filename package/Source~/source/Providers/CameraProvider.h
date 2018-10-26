#pragma once

#include "Unity/IUnityXRCamera.deprecated.h"
#include "Wrappers/WrappedCamera.h"

class CameraProvider : public IUnityXRCameraProvider
{
public:
    CameraProvider();

    const ArCamera* GetArCamera() const;
    ArCamera* GetArCameraMutable();

    float GetScreenWidth() const;
    float GetScreenHeight() const;

    void OnLifecycleInitialize();
    void OnLifecycleShutdown();

    virtual bool UNITY_INTERFACE_API GetFrame(const UnityXRCameraParams& xrParamsIn, UnityXRCameraFrame* xrFrameOut) override;
    virtual void UNITY_INTERFACE_API SetLightEstimationRequested(bool requested) override;
    virtual bool UNITY_INTERFACE_API GetShaderName(char(&shaderName)[kUnityXRStringSize]) override;

    void PopulateCStyleProvider(UnityXRCameraProvider& xrProvider);

    void AcquireCameraFromNewFrame();

private:
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetFrame(UnitySubsystemHandle handle, void* userData, const UnityXRCameraParams* xrParamsIn, UnityXRCameraFrame* xrFrameOut);
    static void UNITY_INTERFACE_API StaticSetLightEstimationRequested(UnitySubsystemHandle handle, void* userData, bool requested);
    static UnitySubsystemErrorCode UNITY_INTERFACE_API StaticGetShaderName(UnitySubsystemHandle handle, void* userData, char shaderName[kUnityXRStringSize]);

    WrappedCameraRaii m_WrappedCamera;
    bool m_LightEstimationEnabled;

    void RetrieveMatricesIfNeeded(const UnityXRCameraParams& xrParamsIn);
    bool m_HaveRetrievedMatrices;
    UnityXRMatrix4x4 m_XrDisplayMatrix;
    UnityXRMatrix4x4 m_XrProjectionMatrix;

    float m_ScreenWidth;
    float m_ScreenHeight;

    float m_CachedZNear = 0;
    float m_CachedZFar = 0;
};
