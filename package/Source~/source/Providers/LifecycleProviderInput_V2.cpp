#include "LifecycleProviderInput_V2.h"
#include "Utility.h"

using namespace UnityXRInput_V2;

const UnityXRInternalInputDeviceId kInputDeviceId = 0;

LifecycleProviderInput_V2::LifecycleProviderInput_V2()
    : m_Initialized(false)
{
}

LifecycleProviderInput_V2::~LifecycleProviderInput_V2()
{
    if (m_Initialized)
        ShutdownImpl();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput_V2::Initialize(IUnitySubsystem* subsystem)
{
    if (m_Initialized)
    {
        DEBUG_LOG_ERROR("Plugin interface is telling the the lifecycle provider for input to initialize when we're already initialized - returning failure for initialization!");
        return kUnitySubsystemErrorCodeFailure;
    }

    IUnityXRInputSubsystem* xrInputSubsystem = static_cast<IUnityXRInputSubsystem*>(subsystem);
    if (nullptr == xrInputSubsystem)
    {
        DEBUG_LOG_ERROR("Failed to get a valid input pointer when initializing, can't run ARCore!");
        return kUnitySubsystemErrorCodeFailure;
    }

    xrInputSubsystem->RegisterProvider(&m_InputProvider);
    m_Initialized = true;
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderInput_V2::Shutdown(IUnitySubsystem* /*subsystem*/)
{
    ShutdownImpl();
}

UnitySubsystemErrorCode UNITY_INTERFACE_API LifecycleProviderInput_V2::Start(IUnitySubsystem* subsystem)
{
    IUnityXRInputSubsystem* xrInputSubsystem = static_cast<IUnityXRInputSubsystem*>(subsystem);
    xrInputSubsystem->DeviceConnected(kInputDeviceId);
    return kUnitySubsystemErrorCodeSuccess;
}

void UNITY_INTERFACE_API LifecycleProviderInput_V2::Stop(IUnitySubsystem* subsystem)
{
    IUnityXRInputSubsystem* xrInputSubsystem = static_cast<IUnityXRInputSubsystem*>(subsystem);
    xrInputSubsystem->DeviceDisconnected(kInputDeviceId);
}

void LifecycleProviderInput_V2::ShutdownImpl()
{
    m_Initialized = false;
}
