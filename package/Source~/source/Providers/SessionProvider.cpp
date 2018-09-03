#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"

#include <GLES2/gl2.h>
#include <GLES2/gl2ext.h>

static JavaVM* s_JavaVM = nullptr;
static JNIEnv* s_JNIEnv = nullptr;

static JNIEnv* GetJNIEnv(JavaVM* javaVM)
{
    JNIEnv* jniEnv = nullptr;

    int getEnvStat = javaVM->GetEnv((void**)&jniEnv, JNI_VERSION_1_2);

    if (getEnvStat == JNI_EDETACHED)
    {
        if (javaVM->AttachCurrentThread(&jniEnv, NULL) != 0)
        {
            DEBUG_LOG_ERROR("Failed to attach current thread to JVM.");
        }
    }
    else if (getEnvStat == JNI_EVERSION)
    {
        DEBUG_LOG_ERROR("Version not supported.");
    }

    return jniEnv;
}

static jobject GetApplicationContext(JNIEnv* jniEnv)
{
    jclass playerClass = jniEnv->FindClass("com/unity3d/player/UnityPlayer");
    jclass activityClass = jniEnv->FindClass("android/app/Activity");

    jfieldID activityField = jniEnv->GetStaticFieldID(playerClass, "currentActivity", "Landroid/app/Activity;");
    jmethodID contextMethod = jniEnv->GetMethodID(activityClass, "getApplicationContext", "()Landroid/content/Context;");

    jobject activity = jniEnv->GetStaticObjectField(playerClass, activityField);
    jobject context = jniEnv->CallObjectMethod(activity, contextMethod);

    return context;
}

jint JNI_OnLoad(JavaVM* vm, void* /*reserved*/)
{
	s_JavaVM = vm;
    s_JNIEnv = GetJNIEnv(vm);    

    return JNI_VERSION_1_2;
}

SessionProvider::SessionProvider()
    : m_CameraTextureName(GL_NONE)
    , m_SessionPausedWithApplication(false)
    , m_DirtyConfigFlags(eConfigFlags::none)
    , m_RequestedConfigFlags(eConfigFlags::none)
{
}

bool SessionProvider::OnLifecycleInitialize()
{
    DEBUG_LOG_FATAL("[SessionProvider::OnLifecycleInitialize] s_JNIEnv: '%p'\n", s_JNIEnv);
    ArStatus ars = m_WrappedSession.TryCreate(s_JNIEnv, GetApplicationContext(s_JNIEnv));
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_FATAL("Failed to create session for ARCore - nothing for ARCore will work! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    m_WrappedConfig.CreateDefault();
    if (m_WrappedConfig == nullptr)
    {
        DEBUG_LOG_FATAL("Unexpected failure to create a config for the ARCore session - nothing for ARCore will work!");
        m_WrappedSession.Release();
        return false;
    }

    if (!m_WrappedConfig.IsSupported())
    {
        DEBUG_LOG_FATAL("Default session configuration provided by the platform isn't supported, even though ARCore documentation says the default will be supported on all ARCore-enabled devices - can't run ARCore!");
        m_WrappedConfig.Release();
        m_WrappedSession.Release();
        return false;
    }

    glGenTextures(1, &m_CameraTextureName);
    glBindTexture(GL_TEXTURE_EXTERNAL_OES, *reinterpret_cast<GLuint*>(&m_CameraTextureName));
    m_WrappedSession.SetCameraTextureName(m_CameraTextureName);

    if (!m_WrappedConfig.TrySetUpdateMode(AR_UPDATE_MODE_LATEST_CAMERA_IMAGE))
        DEBUG_LOG_WARNING("This device doesn't support AR_UPDATE_MODE_LATEST_CAMERA_IMAGE - falling back to default.");

    if (!m_WrappedSession.TryConfigure(m_WrappedConfig))
    {
        // already logged a fatal error from TryConfigure, no need to be even more verbose here
        m_WrappedConfig.Release();
        m_WrappedSession.Release();
        return false;
    }

    m_WrappedFrame.CreateDefault();
    m_SessionPausedWithApplication = false;
    return true;
}

void SessionProvider::OnLifecycleShutdown()
{
    m_WrappedFrame.Release();
    m_WrappedConfig.Release();
    m_WrappedSession.Release();

    if (m_CameraTextureName != GL_NONE)
    {
        glDeleteTextures(1, &m_CameraTextureName);
        m_CameraTextureName = GL_NONE;
    }
}

bool SessionProvider::OnLifecycleStart()
{
    return m_WrappedSession.ConnectOrResume();
}

void SessionProvider::OnLifecycleStop()
{
    m_WrappedSession.DisconnectOrPause();
}

void SessionProvider::RequestStartPlaneRecognition()
{
    m_DirtyConfigFlags |= eConfigFlags::planeRecognition;
    m_RequestedConfigFlags |= eConfigFlags::planeRecognition;
}

void SessionProvider::RequestStopPlaneRecognition()
{
    m_DirtyConfigFlags |= eConfigFlags::planeRecognition;
    m_RequestedConfigFlags &= ~eConfigFlags::planeRecognition;
}

void SessionProvider::RequestStartLightEstimation()
{
    m_DirtyConfigFlags |= eConfigFlags::lightEstimation;
    m_RequestedConfigFlags |= eConfigFlags::lightEstimation;
}

void SessionProvider::RequestStopLightEstimation()
{
    m_DirtyConfigFlags |= eConfigFlags::lightEstimation;
    m_RequestedConfigFlags &= ~eConfigFlags::lightEstimation;
}

const WrappedSession& SessionProvider::GetWrappedSession() const
{
    return m_WrappedSession;
}

WrappedSession& SessionProvider::GetWrappedSessionMutable()
{
    return m_WrappedSession;
}

const WrappedFrame& SessionProvider::GetWrappedFrame() const
{
    return m_WrappedFrame;
}

WrappedFrame& SessionProvider::GetWrappedFrameMutable()
{
    return m_WrappedFrame;
}

bool SessionProvider::IsCurrentConfigSupported() const
{
    return m_WrappedConfig.IsSupported();
}

uint32_t SessionProvider::GetCameraTextureName() const
{
    return m_CameraTextureName;
}

UnityXRTrackingState UNITY_INTERFACE_API SessionProvider::GetTrackingState()
{
    const WrappedCamera& wrappedCamera = GetWrappedCamera();
    ArTrackingState trackingState = wrappedCamera != nullptr ? wrappedCamera.GetTrackingState() : AR_TRACKING_STATE_PAUSED;
    return ConvertGoogleTrackingStateToUnity(trackingState);
}

void UNITY_INTERFACE_API SessionProvider::BeginFrame()
{
}

void UNITY_INTERFACE_API SessionProvider::BeforeRender()
{
    bool configChanged = false;
    if ((m_DirtyConfigFlags & eConfigFlags::planeRecognition) != eConfigFlags::none)
    {
        ArPlaneFindingMode requestedPlaneFindingMode =
            (m_RequestedConfigFlags & eConfigFlags::planeRecognition) != eConfigFlags::none
            ? AR_PLANE_FINDING_MODE_HORIZONTAL : AR_PLANE_FINDING_MODE_DISABLED;
        if (m_WrappedConfig.GetPlaneFindingMode() != requestedPlaneFindingMode)
            configChanged = m_WrappedConfig.TrySetPlaneFindingMode(requestedPlaneFindingMode) || configChanged;
    }
    if ((m_DirtyConfigFlags & eConfigFlags::lightEstimation) != eConfigFlags::none)
    {
        ArLightEstimationMode requestedLightEstimationMode = (m_RequestedConfigFlags & eConfigFlags::lightEstimation) != eConfigFlags::none ? AR_LIGHT_ESTIMATION_MODE_AMBIENT_INTENSITY : AR_LIGHT_ESTIMATION_MODE_DISABLED;
        if (m_WrappedConfig.GetLightEstimationMode() != requestedLightEstimationMode)
            configChanged = m_WrappedConfig.TrySetLightEstimationMode(requestedLightEstimationMode) || configChanged;
    }
    m_DirtyConfigFlags = eConfigFlags::none;

    if (configChanged)
    {
        m_WrappedSession.DisconnectOrPause();
        m_WrappedSession.TryConfigure(m_WrappedConfig);
        m_WrappedSession.ConnectOrResume();
    }

    m_WrappedSession.UpdateAndOverwriteFrame();
    GetWrappedCameraMutable().AcquireFromFrame();
}

void UNITY_INTERFACE_API SessionProvider::ApplicationPaused()
{
    m_SessionPausedWithApplication = m_WrappedSession.IsConnected();
    if (m_SessionPausedWithApplication)
        m_WrappedSession.DisconnectOrPause();
}

void UNITY_INTERFACE_API SessionProvider::ApplicationResumed()
{
    if (!m_SessionPausedWithApplication)
        return;

    m_WrappedSession.ConnectOrResume();
    m_SessionPausedWithApplication = false;
}

SessionProvider::eConfigFlags& operator|=(SessionProvider::eConfigFlags& lhs, SessionProvider::eConfigFlags rhs)
{
    lhs = lhs | rhs;
    return lhs;
}

SessionProvider::eConfigFlags& operator&=(SessionProvider::eConfigFlags& lhs, SessionProvider::eConfigFlags rhs)
{
    lhs = lhs & rhs;
    return lhs;
}

SessionProvider::eConfigFlags operator|(SessionProvider::eConfigFlags lhs, SessionProvider::eConfigFlags rhs)
{
    return EnumCast<SessionProvider::eConfigFlags>(EnumCast<int>(lhs) | EnumCast<int>(rhs));
}

SessionProvider::eConfigFlags operator&(SessionProvider::eConfigFlags lhs, SessionProvider::eConfigFlags rhs)
{
    return EnumCast<SessionProvider::eConfigFlags>(EnumCast<int>(lhs) & EnumCast<int>(rhs));
}

SessionProvider::eConfigFlags operator~(SessionProvider::eConfigFlags flags)
{
    int flipped = ~EnumCast<int>(flags);
    flipped &= EnumCast<int>(SessionProvider::eConfigFlags::all);
    return EnumCast<SessionProvider::eConfigFlags>(flipped);
}
