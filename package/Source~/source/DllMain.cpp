#include "Providers/LifecycleProviderCamera.h"
#include "Providers/LifecycleProviderDepth.h"
#include "Providers/LifecycleProviderInput.h"
#include "Providers/LifecycleProviderInput_V1.h"
#include "Providers/LifecycleProviderPlane.h"
#include "Providers/LifecycleProviderRaycast.h"
#include "Providers/LifecycleProviderReferencePoint.h"
#include "Providers/LifecycleProviderSession.h"
#include "Unity/IUnityInterface.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

static LifecycleProviderCamera s_LifecycleProviderCamera;
static LifecycleProviderDepth s_LifecycleProviderDepth;
static LifecycleProviderInput s_LifecycleProviderInput;
static LifecycleProviderInput_V1 s_LifecycleProviderInput_V1;
static LifecycleProviderPlane s_LifecycleProviderPlane;
static LifecycleProviderRaycast s_LifecycleProviderRaycast;
static LifecycleProviderReferencePoint s_LifecycleProviderReferencePoint;
static LifecycleProviderSession s_LifecycleProviderSession;

const WrappedCamera& GetWrappedCamera()
{
    return s_LifecycleProviderCamera.GetCameraProvider().GetWrappedCamera();
}

WrappedCamera& GetWrappedCameraMutable()
{
    return s_LifecycleProviderCamera.GetCameraProviderMutable().GetWrappedCameraMutable();
}

const WrappedFrame& GetWrappedFrame()
{
    return s_LifecycleProviderSession.GetSessionProvider().GetWrappedFrame();
}

WrappedFrame& GetWrappedFrameMutable()
{
    return s_LifecycleProviderSession.GetSessionProviderMutable().GetWrappedFrameMutable();
}

const WrappedSession& GetWrappedSession()
{
    return s_LifecycleProviderSession.GetSessionProvider().GetWrappedSession();
}

WrappedSession& GetWrappedSessionMutable()
{
    return s_LifecycleProviderSession.GetSessionProviderMutable().GetWrappedSessionMutable();
}

const ArFrame* GetArFrame()
{
    return GetWrappedFrame().Get();
}

ArFrame* GetArFrameMutable()
{
    return GetWrappedFrameMutable().Get();
}

const ArSession* GetArSession()
{
    return GetWrappedSession().Get();
}

ArSession* GetArSessionMutable()
{
    return GetWrappedSessionMutable().Get();
}

const SessionProvider& GetSessionProvider()
{
    return s_LifecycleProviderSession.GetSessionProvider();
}

SessionProvider& GetSessionProviderMutable()
{
    return s_LifecycleProviderSession.GetSessionProviderMutable();
}

uint32_t GetCameraTextureName()
{
    return s_LifecycleProviderSession.GetSessionProvider().GetCameraTextureName();
}

float GetScreenWidth()
{
    return s_LifecycleProviderCamera.GetCameraProvider().GetScreenWidth();
}

float GetScreenHeight()
{
    return s_LifecycleProviderCamera.GetCameraProvider().GetScreenHeight();
}

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
    DEBUG_LOG_ERROR("Hello, world!");
    IUnityXRCameraInterface* xrCameraInterface = unityInterfaces->Get<IUnityXRCameraInterface>();
    if (nullptr == xrCameraInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRCameraInterface - can't even attempt to run ARCore!");
        return;
    }

    IUnityXRDepthInterface* xrDepthInterface = unityInterfaces->Get<IUnityXRDepthInterface>();
    if (nullptr == xrDepthInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRDepthInterface - can't even attempt to run ARCore!");
        return;
    }

    UnityXRInput_V1::IUnityXRInputInterface* xrInputInterface_V1 = nullptr;
    IUnityXRInputInterface* xrInputInterface = unityInterfaces->Get<IUnityXRInputInterface>();
    if (nullptr == xrInputInterface)
    {
        xrInputInterface_V1 = unityInterfaces->Get<UnityXRInput_V1::IUnityXRInputInterface>();
        if(nullptr == xrInputInterface_V1)
        {
            DEBUG_LOG_FATAL("Failed to get IUnityXRInputInterface - can't even attempt to run ARCore!");
            return;
        }
    }
    
    IUnityXRPlaneInterface* xrPlaneInterface = unityInterfaces->Get<IUnityXRPlaneInterface>();
    if (nullptr == xrPlaneInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRPlaneInterface - can't even attempt to run ARCore!");
        return;
    }

    IUnityXRRaycastInterface* xrRaycastInterface = unityInterfaces->Get<IUnityXRRaycastInterface>();
    if (nullptr == xrRaycastInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRRaycastInterface - can't even attempt to run ARCore!");
        return;
    }

    IUnityXRReferencePointInterface* xrReferencePointInterface = unityInterfaces->Get<IUnityXRReferencePointInterface>();
    if (nullptr == xrReferencePointInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRReferencePointInterface - can't even attempt to run ARCore!");
        return;
    }

    IUnityXRSessionInterface* xrSessionInterface = unityInterfaces->Get<IUnityXRSessionInterface>();
    if (nullptr == xrSessionInterface)
    {
        DEBUG_LOG_FATAL("Failed to get IUnityXRSessionInterface - can't even attempt to run ARCore!");
        return;
    }

    bool registered = xrCameraInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Camera", &s_LifecycleProviderCamera);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register camera lifecycle provider - camera pose can't update for this run of ARCore!");

    registered = xrDepthInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Depth", &s_LifecycleProviderDepth);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register depth lifecycle provider - point clouds will be inaccessible for this run of ARCore!");

    if(nullptr != xrInputInterface)
        registered = xrInputInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Input", &s_LifecycleProviderInput);
    else 
        registered = xrInputInterface_V1->RegisterLifecycleProvider("UnityARCore", "ARCore-Input", &s_LifecycleProviderInput_V1);   
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register input lifecycle provider - camera pose can't update for this run of ARCore!");

    registered = xrPlaneInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Plane", &s_LifecycleProviderPlane);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register planes lifecycle provider - planes will be inaccessible for this run of ARCore!");

    registered = xrRaycastInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Raycast", &s_LifecycleProviderRaycast);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register raycast lifecycle provider - raycasting will be inaccessible for this run of ARCore!");

    registered = xrReferencePointInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-ReferencePoint", &s_LifecycleProviderReferencePoint);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register reference point lifecycle provider - reference points will be inaccessible for this run of ARCore!");

    registered = xrSessionInterface->RegisterLifecycleProvider("UnityARCore", "ARCore-Session", &s_LifecycleProviderSession);
    if (!registered)
        DEBUG_LOG_ERROR("Failed to register session lifecycle provider - nothing has a chance of working for this run of ARCore!");
}
