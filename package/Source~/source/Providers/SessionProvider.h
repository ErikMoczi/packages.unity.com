#include "arcore_c_api.h"
#include "Unity/IUnityXRSession.h"
#include "Wrappers/WrappedConfig.h"
#include <jni.h>

class SessionProvider : public IUnityXRSessionProvider
{
public:
    SessionProvider();

    void OnLifecycleInitialize();
    void OnLifecycleShutdown();

    void OnLifecycleStart();
    void OnLifecycleStop();

    void RequestStartPlaneRecognition();
    void RequestStopPlaneRecognition();

    void RequestStartLightEstimation();
    void RequestStopLightEstimation();

    bool IsCurrentConfigSupported() const;

    uint32_t GetCameraTextureName() const;

    virtual UnityXRTrackingState UNITY_INTERFACE_API GetTrackingState() override;
    virtual void UNITY_INTERFACE_API BeginFrame() override;
    virtual void UNITY_INTERFACE_API BeforeRender() override;
    virtual void UNITY_INTERFACE_API ApplicationPaused() override;
    virtual void UNITY_INTERFACE_API ApplicationResumed() override;

private:

    uint32_t m_CameraTextureName;

    enum class eConfigFlags : int
    {
        none             = 0,
        planeRecognition = 1 << 0,
        lightEstimation  = 1 << 1,
        all              = (1 << 2) - 1
    };

    friend eConfigFlags& operator|=(eConfigFlags& lhs, eConfigFlags rhs);
    friend eConfigFlags& operator&=(eConfigFlags& lhs, eConfigFlags rhs);
    friend eConfigFlags operator|(eConfigFlags lhs, eConfigFlags rhs);
    friend eConfigFlags operator&(eConfigFlags lhs, eConfigFlags rhs);
    friend eConfigFlags operator~(eConfigFlags flags);
    eConfigFlags m_DirtyConfigFlags = eConfigFlags::none;
    eConfigFlags m_RequestedConfigFlags = eConfigFlags::none;
};
