#include "Providers/LifecycleProviderCamera.h"
#include "Providers/LifecycleProviderDepth.h"
#include "Providers/LifecycleProviderInput.h"
#include "Providers/LifecycleProviderInput_V1.h"
#include "Providers/LifecycleProviderInput_V2.h"
#include "Providers/LifecycleProviderPlane.h"
#include "Providers/LifecycleProviderRaycast.h"
#include "Providers/LifecycleProviderReferencePoint.h"
#include "Providers/LifecycleProviderSession.h"
#include "Unity/IUnityInterface.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"
#include "Wrappers/WrappedFrame.h"

#include "arpresto_api.h"

static LifecycleProviderCamera s_LifecycleProviderCamera;
static LifecycleProviderDepth s_LifecycleProviderDepth;
static LifecycleProviderInput s_LifecycleProviderInput;
static LifecycleProviderInput_V2 s_LifecycleProviderInput_V2;
static LifecycleProviderInput_V1 s_LifecycleProviderInput_V1;
static LifecycleProviderPlane s_LifecycleProviderPlane;
static LifecycleProviderRaycast s_LifecycleProviderRaycast;
static LifecycleProviderReferencePoint s_LifecycleProviderReferencePoint;
static LifecycleProviderSession s_LifecycleProviderSession;

const ArCamera* GetArCamera()
{
    return s_LifecycleProviderCamera.GetCameraProvider().GetArCamera();
}

ArCamera* GetArCameraMutable()
{
    return s_LifecycleProviderCamera.GetCameraProviderMutable().GetArCameraMutable();
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

int64_t GetLatestTimestamp()
{
    WrappedFrame wrappedFrame = GetArFrame();
    return wrappedFrame.GetTimestamp();
}

template<typename T_LifecycleProvider, typename T_DeprecatedInterface>
UnitySubsystemErrorCode RegisterLifecycleProviderOnDeprecatedInterface(IUnityInterfaces* unityInterfaces, T_LifecycleProvider& lifecycleProvider, const char* subsystemId)
{
    T_DeprecatedInterface* deprecatedInterface = unityInterfaces->Get<T_DeprecatedInterface>();
    if (deprecatedInterface == nullptr)
        return kUnitySubsystemErrorCodeFailure;

    return deprecatedInterface->RegisterLifecycleProvider("UnityARCore", subsystemId, &lifecycleProvider) ? kUnitySubsystemErrorCodeSuccess :  kUnitySubsystemErrorCodeFailure;
}

template<typename T_LifecycleProvider, typename T_UpdatedInterface, typename T_DeprecatedInterface>
UnitySubsystemErrorCode RegisterLifecycleProvider(IUnityInterfaces* unityInterfaces, T_LifecycleProvider& lifecycleProvider, const char* subsystemId)
{
    T_UpdatedInterface* updatedInterface = unityInterfaces->Get<T_UpdatedInterface>();
    if (updatedInterface == nullptr)
        return RegisterLifecycleProviderOnDeprecatedInterface<T_LifecycleProvider, T_DeprecatedInterface>(unityInterfaces, lifecycleProvider, subsystemId);

    return lifecycleProvider.SetUnityInterfaceAndRegister(updatedInterface, subsystemId);
}

#define REGISTER_LIFECYCLE_PROVIDER(SubsystemName) \
do \
{ \
    UnitySubsystemErrorCode registerErrorCode = RegisterLifecycleProvider \
        <LifecycleProvider##SubsystemName, IUnityXR##SubsystemName##Interface, IUnityXR##SubsystemName##Interface_Deprecated> \
        (unityInterfaces, s_LifecycleProvider##SubsystemName, "ARCore-"#SubsystemName); \
    if (registerErrorCode != kUnitySubsystemErrorCodeSuccess) \
        DEBUG_LOG_ERROR("Failed to register lifecycle provider, "#SubsystemName" subsystem will be unavailable!"); \
} \
while (false)

extern "C" void UNITY_INTERFACE_EXPORT UNITY_INTERFACE_API
UnityPluginLoad(IUnityInterfaces* unityInterfaces)
{
	REGISTER_LIFECYCLE_PROVIDER(Camera);
	REGISTER_LIFECYCLE_PROVIDER(Depth);
	REGISTER_LIFECYCLE_PROVIDER(Plane);
	REGISTER_LIFECYCLE_PROVIDER(Raycast);
    REGISTER_LIFECYCLE_PROVIDER(ReferencePoint);
    REGISTER_LIFECYCLE_PROVIDER(Session);

    UnityXRInput_V1::IUnityXRInputInterface* xrInputInterface_V1 = nullptr;
    UnityXRInput_V2::IUnityXRInputInterface* xrInputInterface_V2 = nullptr;
    
    IUnityXRInputInterface* xrInputInterface = unityInterfaces->Get<IUnityXRInputInterface>();
    if (nullptr == xrInputInterface)
    {
        xrInputInterface_V2 = unityInterfaces->Get<UnityXRInput_V2::IUnityXRInputInterface>();
        if (nullptr == xrInputInterface_V2)
        {
            xrInputInterface_V1 = unityInterfaces->Get<UnityXRInput_V1::IUnityXRInputInterface>();
            if(nullptr == xrInputInterface_V1)
            {
                DEBUG_LOG_FATAL("Failed to get IUnityXRInputInterface - can't even attempt to run ARCore!");
                return;
            }
        }
    }

	bool registered = false;
    if (nullptr != xrInputInterface)
        registered = s_LifecycleProviderInput.SetUnityInterfaceAndRegister(xrInputInterface, "ARCore-Input") == kUnitySubsystemErrorCodeSuccess;
    else if (nullptr != xrInputInterface_V2)
        registered = xrInputInterface_V2->RegisterLifecycleProvider("UnityARCore", "ARCore-Input", &s_LifecycleProviderInput_V2);
    else if (nullptr != xrInputInterface_V1)
        registered = xrInputInterface_V1->RegisterLifecycleProvider("UnityARCore", "ARCore-Input", &s_LifecycleProviderInput_V1);   

    if (!registered)
        DEBUG_LOG_ERROR("Failed to register input lifecycle provider - camera pose can't update for this run of ARCore!");
}

void AcquireCameraFromNewFrame()
{
    return s_LifecycleProviderCamera.GetCameraProviderMutable().AcquireCameraFromNewFrame();
}
