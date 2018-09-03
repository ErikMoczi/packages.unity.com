#pragma once

#include "Unity/IUnityXRCamera.deprecated.h"
#include "Wrappers/WrappedCamera.h"

class CameraProvider : public IUnityXRCameraProvider
{
public:
    CameraProvider();

    const WrappedCamera& GetWrappedCamera() const;
    WrappedCamera& GetWrappedCameraMutable();

    float GetScreenWidth() const;
    float GetScreenHeight() const;

    void OnLifecycleShutdown();

    virtual bool UNITY_INTERFACE_API GetFrame(const UnityXRCameraParams& paramsIn, UnityXRCameraFrame* frameOut) override;
    virtual void UNITY_INTERFACE_API SetLightEstimationRequested(bool requested) override;
    virtual bool UNITY_INTERFACE_API GetShaderName(char(&shaderName)[kUnityXRStringSize]) override;

    void PopulateCStyleProvider(UnityXRCameraProvider& provider);

private:
    WrappedCamera m_WrappedCamera;
    bool m_LightEstimationEnabled;

    void RetrieveMatricesIfNeeded(ArSession* session, ArFrame* frame, const UnityXRCameraParams& paramsIn);
    bool m_HaveRetrievedMatrices;
    UnityXRMatrix4x4 m_DisplayMatrix;
    UnityXRMatrix4x4 m_ProjectionMatrix;

    float m_ScreenWidth;
    float m_ScreenHeight;

    float m_CachedZNear = 0;
    float m_CachedZFar = 0;
};
