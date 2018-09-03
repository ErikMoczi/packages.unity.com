#include "SessionProvider.h"
#include "Utility.h"
#include "Wrappers/WrappedCamera.h"
#include "Wrappers/PrestoConfig.h"
#include "arcore_c_api.h"
#include "arpresto_api.h"

#include <GLES2/gl2.h>
#include <GLES2/gl2ext.h>

static JavaVM* s_JavaVM = nullptr;
static JNIEnv* s_JNIEnv = nullptr;
static jobject s_AppActivity = nullptr;
static jobject s_AppContext = nullptr;

jobject CallJavaMethod(jobject object, const char* name, const char* sig)
{
    jclass klass = s_JNIEnv->GetObjectClass(object);
    jmethodID method = s_JNIEnv->GetMethodID(klass, name, sig);
    return s_JNIEnv->CallObjectMethod(object, method);
}

void CameraPermissionRequestProvider(CameraPermissionsResultCallback_FP on_complete, void *context)
{
    // We set this in the AndroidManifest.xml
    // TODO: Handle this in the plugin
    on_complete(true, context);
}

jint JNI_OnLoad(JavaVM* vm, void* /*reserved*/)
{
    auto ret = JNI_VERSION_1_6;

    s_JavaVM = vm;
    if (s_JavaVM == nullptr)
    {
        DEBUG_LOG_FATAL("Invalid java virtual machine.");
        return ret;
    }

    int getEnvStat = s_JavaVM->GetEnv((void**)&s_JNIEnv, JNI_VERSION_1_6);
    if (getEnvStat == JNI_EDETACHED)
    {
        if (s_JavaVM->AttachCurrentThread(&s_JNIEnv, NULL) != 0)
        {
            DEBUG_LOG_FATAL("Failed to attach current thread to JVM.");
            return ret;
        }
    }
    else if (getEnvStat == JNI_EVERSION)
    {
        DEBUG_LOG_FATAL("Version not supported.");
        return ret;
    }

    if (s_JNIEnv == nullptr)
        return ret;

    jclass playerClass = s_JNIEnv->FindClass("com/unity3d/player/UnityPlayer");
    jclass activityClass = s_JNIEnv->FindClass("android/app/Activity");
    jfieldID activityField = s_JNIEnv->GetStaticFieldID(playerClass, "currentActivity", "Landroid/app/Activity;");
    s_AppActivity = s_JNIEnv->GetStaticObjectField(playerClass, activityField);
    s_AppActivity = s_JNIEnv->NewGlobalRef(s_AppActivity);

    if (s_AppActivity == nullptr)
    {
        DEBUG_LOG_FATAL("Could not access the activity.");
        return ret;
    }

    ArPresto_initialize(s_JavaVM, s_AppActivity,
        &CameraPermissionRequestProvider, nullptr, nullptr);

    return ret;
}

SessionProvider::SessionProvider()
    : m_CameraTextureName(GL_NONE)
{ }

void SessionProvider::OnLifecycleInitialize()
{
    // Create OpenGL texture
    glGenTextures(1, &m_CameraTextureName);
    glBindTexture(GL_TEXTURE_EXTERNAL_OES, *reinterpret_cast<GLuint*>(&m_CameraTextureName));
    ArPresto_setCameraTextureName(m_CameraTextureName);

    SetUpdateMode(AR_UPDATE_MODE_LATEST_CAMERA_IMAGE);
}

void SessionProvider::OnLifecycleShutdown()
{
    ArPresto_reset();

    // Cleanup OpenGL texture
    if (m_CameraTextureName != GL_NONE)
    {
        glDeleteTextures(1, &m_CameraTextureName);
        m_CameraTextureName = GL_NONE;
    }
}

void SessionProvider::OnLifecycleStart()
{
    UpdateConfigIfNeeded();
    ArPresto_setEnabled(true);
}

void SessionProvider::OnLifecycleStop()
{
    ArPresto_setEnabled(false);
}

void SessionProvider::RequestStartPlaneRecognition()
{
    m_RequestedConfigFlags |= eConfigFlags::planeRecognition;
}

void SessionProvider::RequestStopPlaneRecognition()
{
    m_RequestedConfigFlags &= ~eConfigFlags::planeRecognition;
}

void SessionProvider::RequestStartLightEstimation()
{
    m_RequestedConfigFlags |= eConfigFlags::lightEstimation;
}

void SessionProvider::RequestStopLightEstimation()
{
    m_RequestedConfigFlags &= ~eConfigFlags::lightEstimation;
}

bool SessionProvider::IsCurrentConfigSupported() const
{
    UpdateConfigIfNeeded();
    ArPrestoStatus status;
    ArPresto_getStatus(&status);

    return status != ARPRESTO_STATUS_ERROR_SESSION_CONFIGURATION_NOT_SUPPORTED;
}

uint32_t SessionProvider::GetCameraTextureName() const
{
    return m_CameraTextureName;
}

UnityXRTrackingState UNITY_INTERFACE_API SessionProvider::GetTrackingState()
{
    if (GetArSession() == nullptr)
        return kUnityXRTrackingStateUnknown;

    const WrappedCamera& wrappedCamera = GetWrappedCamera();
    ArTrackingState trackingState = wrappedCamera != nullptr ? wrappedCamera.GetTrackingState() : AR_TRACKING_STATE_PAUSED;
    return ConvertGoogleTrackingStateToUnity(trackingState);
}

void UNITY_INTERFACE_API SessionProvider::BeginFrame()
{ }

void UNITY_INTERFACE_API SessionProvider::BeforeRender()
{
    const ArPlaneFindingMode requestedPlaneFindingMode =
            (m_RequestedConfigFlags & eConfigFlags::planeRecognition) != eConfigFlags::none
            ? AR_PLANE_FINDING_MODE_HORIZONTAL_AND_VERTICAL : AR_PLANE_FINDING_MODE_DISABLED;

    SetPlaneFindingMode(requestedPlaneFindingMode);

    const ArLightEstimationMode requestedLightEstimationMode =
        (m_RequestedConfigFlags & eConfigFlags::lightEstimation) != eConfigFlags::none
        ? AR_LIGHT_ESTIMATION_MODE_AMBIENT_INTENSITY : AR_LIGHT_ESTIMATION_MODE_DISABLED;

    SetLightEstimationMode(requestedLightEstimationMode);

    UpdateConfigIfNeeded();

    ArPresto_update();

    GetWrappedCameraMutable().AcquireFromFrame();
}

void UNITY_INTERFACE_API SessionProvider::ApplicationPaused()
{
    ArPresto_handleActivityPause();
}

void UNITY_INTERFACE_API SessionProvider::ApplicationResumed()
{
    ArPresto_handleActivityResume();
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
