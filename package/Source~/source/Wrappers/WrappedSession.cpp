#include "Utility.h"
#include "WrappedFrame.h"
#include "WrappedSession.h"

template<>
void WrappingBase<ArSession>::ReleaseImpl()
{
    ArSession_destroy(m_Ptr);
}

WrappedSession::WrappedSession()
    : m_IsConnected(false)
{
}

WrappedSession::~WrappedSession()
{
    DisconnectOrPause();
}

ArStatus WrappedSession::TryCreate(JNIEnv* jniEnv, jobject applicationContext)
{
    if (m_Ptr != nullptr)
        return AR_ERROR_FATAL;

    return ArSession_create(jniEnv, applicationContext, &m_Ptr);
}

bool WrappedSession::TryConfigure(const ArConfig* config)
{
    ArStatus ars = ArSession_configure(m_Ptr, config);
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_FATAL("Failed to set configuration, even though we checked that it was supported - cannot start ARCore! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    return true;
}

int64_t WrappedSession::GetTimestamp() const
{
    int64_t timestamp = 0;
    WrappedFrame& frame = GetWrappedFrameMutable();
    ArFrame_getTimestamp(m_Ptr, frame.Get(), &timestamp);

    return timestamp;
}

bool WrappedSession::UpdateAndOverwriteFrame()
{
    if (!m_IsConnected)
        return false;

    WrappedFrame& frame = GetWrappedFrameMutable();
    ArStatus ars = ArSession_update(m_Ptr, frame.Get());
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_ERROR("Failed to update session! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    return true;
}

void WrappedSession::SetCameraTextureName(uint32_t textureId)
{
    ArSession_setCameraTextureName(m_Ptr, textureId);
}

void WrappedSession::SetDisplayGeometry(int rotation, int width, int height)
{
    ArSession_setDisplayGeometry(m_Ptr, rotation, width, height);
}

bool WrappedSession::ConnectOrResume()
{
    if (m_IsConnected)
        return true;

    ArStatus ars = ArSession_resume(m_Ptr);
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_ERROR("Failed to start or resume session! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    m_IsConnected = true;
    return true;
}

bool WrappedSession::DisconnectOrPause()
{
    if (!m_IsConnected)
        return true;

    ArStatus ars = ArSession_pause(m_Ptr);
    if (ARSTATUS_FAILED(ars))
    {
        DEBUG_LOG_ERROR("Failed to pause or disconnect session! Error: '%s'.", PrintArStatus(ars));
        return false;
    }

    m_IsConnected = false;
    return true;
}

bool WrappedSession::IsConnected() const
{
    return m_IsConnected;
}
