#include "LifecycleProviderEnvironment.h"
#include "Utility.h"
#include "Unity/IUnityXRTypes.h"

LifecycleProviderEnvironment::LifecycleProviderEnvironment()
    : m_Initialized(false)
{
}

LifecycleProviderEnvironment::~LifecycleProviderEnvironment()
{
    if (m_Initialized)
        ShutdownImpl();
}

XRErrorCode UNITY_INTERFACE_API LifecycleProviderEnvironment::Initialize(IXRInstance* xrInstance)
{
    if (m_Initialized)
    {
        DEBUG_LOG_ERROR("Plugin interface is telling the lifecycle provider for environment to initialize when we're already initialized - returning failure for initialization!");
        return kXRErrorCodeFailure;
    }

    IUnityXREnvironmentInstance* environmentInstance = static_cast<IUnityXREnvironmentInstance*>(xrInstance);
    if (environmentInstance == nullptr)
    {
        DEBUG_LOG_FATAL("Failed to get a valid environment pointer when initializing, can't run ARCore!");
        return kXRErrorCodeFailure;
    }

    if (!m_SessionProvider.OnLifecycleInitialize())
        return kXRErrorCodeFailure;

    environmentInstance->RegisterDepthProvider(&m_DepthProvider);
    environmentInstance->RegisterPlaneProvider(&m_PlaneProvider);
    environmentInstance->RegisterRaycastProvider(&m_RaycastProvider);
    environmentInstance->RegisterReferencePointProvider(&m_ReferencePointProvider);
    environmentInstance->RegisterSessionProvider(&m_SessionProvider);

    m_Initialized = true;
    return kXRErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderEnvironment::Shutdown(IXRInstance* /*xrInstance*/)
{
    ShutdownImpl();
}

// SessionProvider::Connect called automatically by XREnvironment::Start
XRErrorCode UNITY_INTERFACE_API LifecycleProviderEnvironment::Start(IXRInstance* xrInstance)
{
    if (!m_SessionProvider.IsCurrentConfigSupported())
        return kXRErrorCodeFailure;

    return kXRErrorCodeSuccess;
}

// SessionProvider::Disconnect called automatically by XREnvironment::Stop
void UNITY_INTERFACE_API LifecycleProviderEnvironment::Stop(IXRInstance* xrInstance)
{
}

const SessionProvider& LifecycleProviderEnvironment::GetSessionProvider() const
{
    return m_SessionProvider;
}

SessionProvider& LifecycleProviderEnvironment::GetSessionProviderMutable()
{
    return m_SessionProvider;
}

void LifecycleProviderEnvironment::ShutdownImpl()
{
    m_SessionProvider.OnLifecycleShutdown();
}
